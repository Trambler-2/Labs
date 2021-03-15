﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Library.Graph.Views
{
    /// <summary>
    /// Представляет элемент представления в виде списка смежности.
    /// </summary>
    /// <typeparam name="TValue">Тип элементов.</typeparam>
    public readonly struct AdjacensyViewItem<TValue> : IGraphViewItem<TValue>
        where TValue : IEquatable<TValue>
    {
        /// <summary>
        /// Возвращает вершину списка смежности.
        /// </summary>
        public TValue Vertex { get; }

        /// <summary>
        /// Возвращает список вершин смежных с <see cref="Vertex"/>.
        /// </summary>
        public IReadOnlyList<TValue> Items { get; }

        /// <summary>
        /// Конструктор элемента списка смежности.
        /// </summary>
        /// <param name="vertex">Представляет вершину списка смежности.</param>
        /// <param name="items">Представляет список вершин смежных с <param name="vertex"/>.</param>
        public AdjacensyViewItem(TValue vertex, IEnumerable<TValue> items)
        {
            if (items is null)
            {
                throw new ArgumentNullException(nameof(items));
            }
            Vertex = vertex ?? throw new ArgumentNullException(nameof(vertex));
            Items = items.ToList();
        }

        public override string ToString()
        {
            if (Items.Any())
            {
                return $"{Vertex} : {string.Join(" , ", Items)}";
            }
            return $"{Vertex} : Empty";
        }
    }
}