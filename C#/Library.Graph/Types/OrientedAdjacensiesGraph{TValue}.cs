﻿using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

using OfficeOpenXml;

using Library.Graph.Views;
using Library.Graph.ConvertibleTypes;

namespace Library.Graph.Types
{
    /// <summary>
    /// Представляет реализацию ориентированного графа на списках смежности.
    /// </summary>
    /// <typeparam name="TValue">Тип элементов графа.</typeparam>
    public sealed class OrientedAdjacensiesGraph<TValue> : ImportableExportableGraph<OrientedAdjacensiesView<TValue>, AdjacensyViewItem<TValue>, TValue>
        where TValue : IEquatable<TValue>, IStringConvertible<TValue>, new()
    {
        /// <summary>
        /// Конструктор графа.
        /// </summary>
        /// <param name="edgeType">Тип ребер графа.</param>
        public OrientedAdjacensiesGraph()
            : base(EdgeType.Directed) { }

        /// <summary>
        /// Конструктор графа.
        /// </summary>
        /// <param name="view">Представления ребер на списках смежности.</param>
        /// <param name="edgeType">Тип ребер графа.</param>
        public OrientedAdjacensiesGraph(OrientedAdjacensiesView<TValue> view)
            : base(view, EdgeType.Directed)
        {
        }

        /// <summary>
        /// Возвращает сгенерированный слабо связный граф с количеством вершин равным <paramref name="vertices"/>
        /// и средней степенью вершин равной <paramref name="meanCohesionPower"/>.
        /// </summary>
        /// <param name="vertices">Количество вершин.</param>
        /// <param name="meanCohesionPower">Средняя степень вершин.</param>
        /// <param name="factory">Фабрика элементов.</param>
        public static OrientedAdjacensiesGraph<TValue> GenerateWithWeakCohesion(int vertices, int meanCohesionPower, Func<TValue> factory)
        {
            InitializeVerticesSetAndMap(vertices, meanCohesionPower, factory);
            InitializeCoherentMapCore();
            return Build();
        }

        /// <summary>
        /// Возвращает сгенерированный несвязный граф с количеством вершин равным <paramref name="vertices"/>
        /// и средней степенью вершин равной <paramref name="meanCohesionPower"/>.
        /// </summary>
        /// <param name="vertices">Количество вершин.</param>
        /// <param name="meanCohesionPower">Средняя степень вершин.</param>
        /// <param name="factory">Фабрика элементов.</param>
        public static OrientedAdjacensiesGraph<TValue> GenerateInCoherent(int vertices, int meanCohesionPower, Func<TValue> factory)
        {
            InitializeVerticesSetAndMap(vertices, meanCohesionPower, factory);
            InitializeInCoherentMapCore();
            return Build();
        }

        protected override async Task<string> ExportCoreAsync(string fileName)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using var package = new ExcelPackage(new FileInfo(fileName));
            var worksheet = package.Workbook.Worksheets.Add("GRAPH_DUMP");

            worksheet.Cells[1, 1].Value = "Source";
            worksheet.Cells[1, 2].Value = "Target";
            worksheet.Cells[1, 4].Value = "Type";
            worksheet.Cells[1, 5].Value = "Label";

            var edges = View.Items.Select(
                c => c.Items.Select(
                    v => new EdgeViewItem<TValue>(c.Vertex, v)))
                .SelectMany(s => s)
                .ToList();

            for (int i = 0; i < edges.Count; i++)
            {
                worksheet.Cells[i + 2, 1].Value = edges[i].First.ToString();
                worksheet.Cells[i + 2, 2].Value = edges[i].Second.ToString();
                worksheet.Cells[i + 2, 4].Value = EdgeType;
                worksheet.Cells[i + 2, 5].Value = $"From '{edges[i].First}' to '{edges[i].Second}'";
            }

            await package.SaveAsync();

            return fileName;
        }

        protected override async Task ImportCoreAsync(string fileName)
        {
            await Task.Yield();

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using var package = new ExcelPackage(new FileInfo(fileName));
            var worksheet = package.Workbook.Worksheets[0];

            var mapVertexAndItems = new Dictionary<TValue, List<TValue>>();

            for (int i = 2; i <= worksheet.Dimension.End.Row; i++) // int i = 1 Skip headers
            {
                var entity = new TValue();
                var edge = new EdgeViewItem<TValue>(
                    entity.ConvertFromString(worksheet.Cells[i, 1].Value.ToString()),
                    entity.ConvertFromString(worksheet.Cells[i, 2].Value.ToString()));

                if (!mapVertexAndItems.ContainsKey(edge.First))
                {
                    mapVertexAndItems.Add(edge.First, new List<TValue>());
                }
                else
                {
                    mapVertexAndItems[edge.First].Add(edge.Second);
                }
            }
            VerticesSet = mapVertexAndItems.Keys.ToList();

            View = new OrientedAdjacensiesView<TValue>(
                mapVertexAndItems.Select(item => new AdjacensyViewItem<TValue>(item.Key, item.Value)));
        }

        private static void InitializeCoherentMapCore()
        {
            _ = MapVertexAndLists
                .Aggregate((f, s) =>
                {
                    s.Value.Items.Add(f.Key);
                    return s;
                });
            foreach (var pair in MapVertexAndLists)
            {
                _ = Enumerable
                    .Range(0, pair.Value.Count)
                    .Aggregate((ff, ss) =>
                    {
                        while (pair.Value.Items.Count < pair.Value.Count)
                        {
                            var addedVertex = VerticesSet[RandomGenerator.Next(VerticesSet.Count)];

                            if (!pair.Value.Items.Contains(addedVertex) && !addedVertex.Equals(pair.Key))
                            {
                                _ = pair.Value.Items.Add(addedVertex);
                            }
                        }
                        return ss;
                    });
            }
        }

        private static void InitializeInCoherentMapCore()
        {
            var skipVertex = VerticesSet[RandomGenerator.Next(VerticesSet.Count)];

            foreach (var pair in MapVertexAndLists)
            {
                if (pair.Key.Equals(skipVertex))
                {
                    continue;
                }

                _ = Enumerable
                    .Range(0, pair.Value.Count + 1)
                    .Aggregate((ff, ss) =>
                    {
                        while (pair.Value.Items.Count < pair.Value.Count)
                        {
                            var addedVertex = VerticesSet[RandomGenerator.Next(VerticesSet.Count)];

                            if (!addedVertex.Equals(skipVertex) && !pair.Value.Items.Contains(addedVertex) && !addedVertex.Equals(pair.Key))
                            {
                                _ = pair.Value.Items.Add(addedVertex);
                            }
                        }
                        return ss;
                    });
            }
        }

        private static OrientedAdjacensiesGraph<TValue> Build()
            => new OrientedAdjacensiesGraph<TValue>(
                new OrientedAdjacensiesView<TValue>(
                    MapVertexAndLists
                    .Select(kv => new AdjacensyViewItem<TValue>(kv.Key, kv.Value.Items))
                    .ToList()));
    }
}