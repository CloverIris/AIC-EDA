using AIC_EDA.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Windows.Storage;

namespace AIC_EDA.Services
{
    /// <summary>
    /// 工厂布局序列化器 - JSON 保存/加载
    /// </summary>
    public static class LayoutSerializer
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        public static string Serialize(FactoryLayout layout)
        {
            var dto = new LayoutDto
            {
                Id = layout.Id,
                Name = layout.Name,
                CreatedAt = layout.CreatedAt,
                ModifiedAt = layout.ModifiedAt,
                GridSize = layout.GridSize,
                CanvasGridWidth = layout.CanvasGridWidth,
                CanvasGridHeight = layout.CanvasGridHeight,
                Machines = layout.Machines.ConvertAll(m => new MachineDto
                {
                    Id = m.Id,
                    MachineType = m.MachineType.ToString(),
                    GridX = m.GridX,
                    GridY = m.GridY,
                    Rotation = m.Rotation,
                    Label = m.Label,
                    RecipeId = m.RecipeId,
                }),
                Connections = layout.Connections.ConvertAll(c => new ConnectionDto
                {
                    Id = c.Id,
                    SourceId = c.SourceId,
                    TargetId = c.TargetId,
                    ItemId = c.ItemId,
                    Type = c.Type.ToString(),
                }),
            };
            return JsonSerializer.Serialize(dto, JsonOptions);
        }

        public static FactoryLayout Deserialize(string json)
        {
            var dto = JsonSerializer.Deserialize<LayoutDto>(json, JsonOptions);
            if (dto == null) throw new InvalidOperationException("Failed to deserialize layout.");

            var layout = new FactoryLayout
            {
                Id = dto.Id,
                Name = dto.Name,
                CreatedAt = dto.CreatedAt,
                ModifiedAt = dto.ModifiedAt,
                GridSize = dto.GridSize,
                CanvasGridWidth = dto.CanvasGridWidth,
                CanvasGridHeight = dto.CanvasGridHeight,
            };

            foreach (var md in dto.Machines)
            {
                if (Enum.TryParse<MachineType>(md.MachineType, out var mt))
                {
                    layout.Machines.Add(new PlacedMachine
                    {
                        Id = md.Id,
                        MachineType = mt,
                        GridX = md.GridX,
                        GridY = md.GridY,
                        Rotation = md.Rotation,
                        Label = md.Label,
                        RecipeId = md.RecipeId,
                    });
                }
            }

            foreach (var cd in dto.Connections)
            {
                if (Enum.TryParse<ConnectionType>(cd.Type, out var ct))
                {
                    layout.Connections.Add(new MachineConnection
                    {
                        Id = cd.Id,
                        SourceId = cd.SourceId,
                        TargetId = cd.TargetId,
                        ItemId = cd.ItemId,
                        Type = ct,
                    });
                }
            }

            return layout;
        }

        public static async Task SaveToFileAsync(FactoryLayout layout, StorageFile file)
        {
            var json = Serialize(layout);
            await FileIO.WriteTextAsync(file, json);
        }

        public static async Task<FactoryLayout> LoadFromFileAsync(StorageFile file)
        {
            var json = await FileIO.ReadTextAsync(file);
            return Deserialize(json);
        }

        // DTOs for serialization
        private class LayoutDto
        {
            public Guid Id { get; set; }
            public string Name { get; set; } = "";
            public DateTime CreatedAt { get; set; }
            public DateTime ModifiedAt { get; set; }
            public int GridSize { get; set; }
            public int CanvasGridWidth { get; set; }
            public int CanvasGridHeight { get; set; }
            public List<MachineDto> Machines { get; set; } = new();
            public List<ConnectionDto> Connections { get; set; } = new();
        }

        private class MachineDto
        {
            public Guid Id { get; set; }
            public string MachineType { get; set; } = "";
            public int GridX { get; set; }
            public int GridY { get; set; }
            public int Rotation { get; set; }
            public string? Label { get; set; }
            public string? RecipeId { get; set; }
        }

        private class ConnectionDto
        {
            public Guid Id { get; set; }
            public Guid SourceId { get; set; }
            public Guid TargetId { get; set; }
            public string? ItemId { get; set; }
            public string Type { get; set; } = "";
        }
    }
}
