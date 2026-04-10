using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 权重表
/// </summary>
public class WeightedTable<T> : IEnumerable<KeyValuePair<T, double>>
{
    private readonly List<WeightedItem> _items;
    private double _totalWeight;
    private readonly Random _random;
    private bool _isDirty = false;

    private struct WeightedItem
    {
        public T Item { get; set; }
        public double Weight { get; set; }

        public WeightedItem(T item, double weight)
        {
            Item = item;
            Weight = weight;
        }
    }

    /// <summary>
    /// 获取或设置权重表名称
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// 总权重
    /// </summary>
    public double TotalWeight
    {
        get
        {
            if (_isDirty)
                RecalculateTotalWeight();
            return _totalWeight;
        }
    }

    /// <summary>
    /// 元素数量
    /// </summary>
    public int Count => _items.Count;

    /// <summary>
    /// 初始化权重表
    /// </summary>
    /// <param name="name">表名称</param>
    /// <param name="seed">随机种子</param>
    public WeightedTable(string name = null, int? seed = null)
    {
        _items = new List<WeightedItem>();
        _totalWeight = 0;
        Name = name ?? "WeightedTable";
        _random = seed.HasValue ? new Random(seed.Value) : new Random();
    }

    /// <summary>
    /// 从字典初始化权重表
    /// </summary>
    public WeightedTable(IDictionary<T, double> items, string name = null, int? seed = null)
        : this(name, seed)
    {
        foreach (var kvp in items)
        {
            Add(kvp.Key, kvp.Value);
        }
    }

    /// <summary>
    /// 添加元素和权重
    /// </summary>
    public void Add(T item, double weight)
    {
        if (weight <= 0)
            throw new ArgumentException("Weight must be greater than 0", nameof(weight));

        _items.Add(new WeightedItem(item, weight));
        _totalWeight += weight;
    }

    /// <summary>
    /// 批量添加元素
    /// </summary>
    public void AddRange(IEnumerable<KeyValuePair<T, double>> items)
    {
        foreach (var item in items)
        {
            Add(item.Key, item.Value);
        }
    }

    /// <summary>
    /// 更新指定元素的权重
    /// </summary>
    public bool UpdateWeight(T item, double newWeight)
    {
        if (newWeight <= 0)
            throw new ArgumentException("Weight must be greater than 0", nameof(newWeight));

        for (int i = 0; i < _items.Count; i++)
        {
            if (Equals(_items[i].Item, item))
            {
                var oldWeight = _items[i].Weight;
                _items[i] = new WeightedItem(item, newWeight);
                _totalWeight += newWeight - oldWeight;
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 移除指定元素
    /// </summary>
    public bool Remove(T item)
    {
        for (int i = 0; i < _items.Count; i++)
        {
            if (Equals(_items[i].Item, item))
            {
                var weight = _items[i].Weight;
                _items.RemoveAt(i);
                _totalWeight -= weight;
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 获取指定元素的权重
    /// </summary>
    public double GetWeight(T item)
    {
        foreach (var weightedItem in _items)
        {
            if (Equals(weightedItem.Item, item))
                return weightedItem.Weight;
        }
        throw new KeyNotFoundException($"Item {item} not found in weighted table");
    }

    /// <summary>
    /// 获取元素的概率（权重/总权重）
    /// </summary>
    public double GetProbability(T item)
    {
        var weight = GetWeight(item);
        return weight / TotalWeight;
    }

    /// <summary>
    /// 检查是否包含指定元素
    /// </summary>
    public bool Contains(T item)
    {
        return _items.Any(x => Equals(x.Item, item));
    }

    /// <summary>
    /// 按权重随机选择一个元素
    /// </summary>
    public T RandomSelect()
    {
        if (_items.Count == 0)
            throw new InvalidOperationException("Weighted table is empty");

        if (_isDirty)
            RecalculateTotalWeight();

        var target = _random.NextDouble() * _totalWeight;
        double cumulative = 0;

        foreach (var item in _items)
        {
            cumulative += item.Weight;
            if (cumulative >= target)
                return item.Item;
        }

        // 理论上不会执行到这里，但为了安全返回最后一个元素
        return _items[^1].Item;
    }

    /// <summary>
    /// 按权重随机选择多个元素（可重复）
    /// </summary>
    public IEnumerable<T> RandomSelectMultiple(int count)
    {
        if (count <= 0)
            throw new ArgumentException("Count must be greater than 0", nameof(count));

        for (int i = 0; i < count; i++)
        {
            yield return RandomSelect();
        }
    }

    /// <summary>
    /// 按权重随机选择多个不重复的元素
    /// </summary>
    public IEnumerable<T> RandomSelectDistinct(int count)
    {
        if (count > _items.Count)
            throw new ArgumentException($"Cannot select {count} distinct items from table with only {_items.Count} items");

        var selectedIndices = new HashSet<int>();
        var results = new List<T>();

        while (results.Count < count)
        {
            var item = RandomSelect();
            var index = _items.FindIndex(x => Equals(x.Item, item));

            if (!selectedIndices.Contains(index))
            {
                selectedIndices.Add(index);
                results.Add(item);
            }
        }

        return results;
    }

    /// <summary>
    /// 按权重随机选择一个元素并移除
    /// </summary>
    public T RandomPopOne()
    {
        if (_items.Count == 0)
            throw new InvalidOperationException("Weighted table is empty");

        if (_isDirty)
            RecalculateTotalWeight();

        var target = _random.NextDouble() * _totalWeight;
        double cumulative = 0;

        foreach (var item in _items)
        {
            cumulative += item.Weight;
            if (cumulative >= target)
            {
                var result = item.Item;
                _items.Remove(item);
                return result;
            } 
        }
        // 理论上不会执行到这里，但为了安全返回第一个元素
        var ret = _items[0].Item;
        _items.RemoveAt(0);
        return ret;
    }

    /// <summary>
    /// 按权重排序（从高到低）
    /// </summary>
    public IEnumerable<KeyValuePair<T, double>> GetSortedByWeightDescending()
    {
        return _items
            .OrderByDescending(x => x.Weight)
            .Select(x => new KeyValuePair<T, double>(x.Item, x.Weight));
    }

    /// <summary>
    /// 获取所有元素及其权重
    /// </summary>
    public IReadOnlyList<KeyValuePair<T, double>> GetAllItems()
    {
        return _items.Select(x => new KeyValuePair<T, double>(x.Item, x.Weight)).ToList();
    }

    /// <summary>
    /// 清空权重表
    /// </summary>
    public void Clear()
    {
        _items.Clear();
        _totalWeight = 0;
    }

    /// <summary>
    /// 归一化所有权重（使总权重为1）
    /// </summary>
    public void Normalize()
    {
        if (_totalWeight == 0)
            return;

        var factor = 1.0 / _totalWeight;
        for (int i = 0; i < _items.Count; i++)
        {
            var item = _items[i];
            _items[i] = new WeightedItem(item.Item, item.Weight * factor);
        }
        _totalWeight = 1.0;
    }

    /// <summary>
    /// 按比例缩放所有权重
    /// </summary>
    public void ScaleWeights(double factor)
    {
        if (factor <= 0)
            throw new ArgumentException("Factor must be greater than 0", nameof(factor));

        for (int i = 0; i < _items.Count; i++)
        {
            var item = _items[i];
            _items[i] = new WeightedItem(item.Item, item.Weight * factor);
        }
        _totalWeight *= factor;
    }

    /// <summary>
    /// 转换为字典
    /// </summary>
    public Dictionary<T, double> ToDictionary()
    {
        return _items.ToDictionary(x => x.Item, x => x.Weight);
    }

    private void RecalculateTotalWeight()
    {
        _totalWeight = _items.Sum(x => x.Weight);
        _isDirty = false;
    }

    /// <summary>
    /// 获取枚举器
    /// </summary>
    public IEnumerator<KeyValuePair<T, double>> GetEnumerator()
    {
        return _items
            .Select(x => new KeyValuePair<T, double>(x.Item, x.Weight))
            .GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    /// <summary>
    /// 重写ToString方法
    /// </summary>
    public override string ToString()
    {
        return $"{Name} (Count: {Count}, TotalWeight: {TotalWeight:F2})";
    }

    /// <summary>
    /// 获取详细统计信息
    /// </summary>
    public string GetStatistics()
    {
        var stats = new System.Text.StringBuilder();
        stats.AppendLine($"Weight Table: {Name}");
        stats.AppendLine($"Total Items: {Count}");
        stats.AppendLine($"Total Weight: {TotalWeight:F4}");
        stats.AppendLine();
        stats.AppendLine("Items (sorted by weight):");
        stats.AppendLine("-------------------------");

        var sortedItems = GetSortedByWeightDescending().ToList();
        foreach (var item in sortedItems)
        {
            var probability = item.Value / TotalWeight * 100;
            stats.AppendLine($"{item.Key}: Weight={item.Value:F2} ({probability:F2}%)");
        }

        return stats.ToString();
    }
}
