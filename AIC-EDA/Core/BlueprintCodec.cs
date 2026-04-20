using AIC_EDA.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AIC_EDA.Core
{
    /// <summary>
    /// Tape-Deploy（部署流片）- 蓝图编解码器
    /// 类比IC设计中的GDSII格式交付
    /// </summary>
    public class BlueprintCodec
    {
        public const string BlueprintHeader = "AIC-EDA-BP";
        public const string Version = "1.0";

        /// <summary>
        /// 内部蓝图数据结构
        /// </summary>
        public class BlueprintData
        {
            public string SchemaVersion { get; set; } = BlueprintCodec.Version;
            public string CreatedAt { get; set; } = DateTime.UtcNow.ToString("O");
            public string TargetItem { get; set; } = string.Empty;
            public double TargetRate { get; set; }
            public List<BlueprintMachine> Machines { get; set; } = new();
            public List<BlueprintBelt> Belts { get; set; } = new();
            public List<BlueprintPower> Power { get; set; } = new();
            public List<string> Tags { get; set; } = new();
        }

        public class BlueprintMachine
        {
            public string Type { get; set; } = string.Empty;
            public double X { get; set; }
            public double Y { get; set; }
            public double Z { get; set; }
            public int Rotation { get; set; }
            public string Recipe { get; set; } = string.Empty;
            public int Count { get; set; } = 1;
        }

        public class BlueprintBelt
        {
            public double FromX { get; set; }
            public double FromY { get; set; }
            public double FromZ { get; set; }
            public double ToX { get; set; }
            public double ToY { get; set; }
            public double ToZ { get; set; }
            public string Item { get; set; } = string.Empty;
            public string Tier { get; set; } = "Basic";
        }

        public class BlueprintPower
        {
            public string Type { get; set; } = string.Empty;
            public double X { get; set; }
            public double Y { get; set; }
            public double Z { get; set; }
        }

        /// <summary>
        /// 将生产图编码为蓝图字符串
        /// </summary>
        public string Encode(ProductionGraph graph)
        {
            var data = new BlueprintData
            {
                TargetItem = graph.TargetItem,
                TargetRate = graph.TargetRate,
            };

            // 编码设备
            foreach (var node in graph.Nodes)
            {
                if (node.Position == null) continue;

                data.Machines.Add(new BlueprintMachine
                {
                    Type = node.Recipe.Machine.ToString(),
                    X = node.Position.Value.X,
                    Y = node.Position.Value.Y,
                    Z = node.Position.Value.Z,
                    Rotation = node.Rotation,
                    Recipe = node.Recipe.Id,
                    Count = node.Count
                });
            }

            // 编码传送带
            foreach (var edge in graph.Edges)
            {
                var source = graph.FindNode(edge.SourceId);
                var target = graph.FindNode(edge.TargetId);
                if (source?.Position == null || target?.Position == null) continue;

                data.Belts.Add(new BlueprintBelt
                {
                    FromX = source.Position.Value.X,
                    FromY = source.Position.Value.Y,
                    FromZ = source.Position.Value.Z,
                    ToX = target.Position.Value.X,
                    ToY = target.Position.Value.Y,
                    ToZ = target.Position.Value.Z,
                    Item = edge.ItemId,
                    Tier = SelectBeltTier(edge.RatePerMinute)
                });
            }

            // JSON序列化
            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions
            {
                WriteIndented = false,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            // 压缩
            var compressed = Compress(json);
            var encoded = Convert.ToBase64String(compressed);

            // 添加头
            return $"{BlueprintHeader}|{Version}|{encoded}";
        }

        /// <summary>
        /// 解码蓝图字符串
        /// </summary>
        public BlueprintData? Decode(string blueprintString)
        {
            if (!blueprintString.StartsWith(BlueprintHeader))
                throw new ArgumentException("Invalid blueprint format");

            var parts = blueprintString.Split('|');
            if (parts.Length < 3)
                throw new ArgumentException("Invalid blueprint format");

            var version = parts[1];
            var encoded = parts[2];

            var compressed = Convert.FromBase64String(encoded);
            var json = Decompress(compressed);

            return JsonSerializer.Deserialize<BlueprintData>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }

        /// <summary>
        /// 导出为JSON文件
        /// </summary>
        public async Task ExportToJsonAsync(ProductionGraph graph, string filePath)
        {
            var data = new BlueprintData
            {
                TargetItem = graph.TargetItem,
                TargetRate = graph.TargetRate,
            };

            foreach (var node in graph.Nodes)
            {
                if (node.Position == null) continue;
                data.Machines.Add(new BlueprintMachine
                {
                    Type = node.Recipe.Machine.ToString(),
                    X = node.Position.Value.X,
                    Y = node.Position.Value.Y,
                    Z = node.Position.Value.Z,
                    Rotation = node.Rotation,
                    Recipe = node.Recipe.Id,
                    Count = node.Count
                });
            }

            foreach (var edge in graph.Edges)
            {
                var source = graph.FindNode(edge.SourceId);
                var target = graph.FindNode(edge.TargetId);
                if (source?.Position == null || target?.Position == null) continue;

                data.Belts.Add(new BlueprintBelt
                {
                    FromX = source.Position.Value.X,
                    FromY = source.Position.Value.Y,
                    FromZ = source.Position.Value.Z,
                    ToX = target.Position.Value.X,
                    ToY = target.Position.Value.Y,
                    ToZ = target.Position.Value.Z,
                    Item = edge.ItemId,
                    Tier = SelectBeltTier(edge.RatePerMinute)
                });
            }

            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(filePath, json);
        }

        /// <summary>
        /// 根据流量选择传送带等级
        /// </summary>
        private string SelectBeltTier(double ratePerMinute)
        {
            return ratePerMinute switch
            {
                <= 30 => "Basic",
                <= 60 => "Advanced",
                <= 120 => "HighSpeed",
                _ => "Express"
            };
        }

        private byte[] Compress(string input)
        {
            var inputBytes = Encoding.UTF8.GetBytes(input);
            using var outputStream = new MemoryStream();
            using (var gzipStream = new GZipStream(outputStream, CompressionMode.Compress))
            {
                gzipStream.Write(inputBytes, 0, inputBytes.Length);
            }
            return outputStream.ToArray();
        }

        private string Decompress(byte[] input)
        {
            using var inputStream = new MemoryStream(input);
            using var gzipStream = new GZipStream(inputStream, CompressionMode.Decompress);
            using var outputStream = new MemoryStream();
            gzipStream.CopyTo(outputStream);
            return Encoding.UTF8.GetString(outputStream.ToArray());
        }
    }
}
