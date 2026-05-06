using Ares.Datamodel;

namespace UI.Features.DataExplorer;

public sealed record DataBlock
{
  public required string Name { get; init; }

  public IReadOnlyList<DataColumn> Columns { get; init; } = [];

  public IReadOnlyList<DataRow> Rows { get; init; } = [];
}

public sealed record DataColumn
{
  public required string Key { get; init; }

  public required string Header { get; init; }

  public required AresDataType Type { get; init; }

  public string? Unit { get; init; }
}

public sealed record DataRow
{
  public IReadOnlyDictionary<string, AresValue> Values { get; init; } = new Dictionary<string, AresValue>();
}

public sealed class MutableDataBlock
{
  public required string Name { get; set; }

  public List<DataColumn> Columns { get; } = [];

  public List<MutableDataRow> Rows { get; } = [];

  public void RemoveColumn(string key)
  {
    Columns.RemoveAll(column => column.Key == key);

    foreach(var row in Rows)
    {
      row.Values.Remove(key);
    }
  }

  public bool RemoveRow(MutableDataRow row)
  {
    return Rows.Remove(row);
  }

  public void SetValue(MutableDataRow row, string columnKey, AresValue value)
  {
    if(!Rows.Contains(row))
      throw new ArgumentException("Row does not belong to this data block.", nameof(row));

    if(Columns.All(column => column.Key != columnKey))
      throw new ArgumentException("Column does not belong to this data block.", nameof(columnKey));

    row.Values[columnKey] = value.Clone();
  }

  public DataBlock ToImmutable()
  {
    return new DataBlock
    {
      Name = Name,
      Columns = Columns.ToArray(),
      Rows = Rows
        .Select(row => new DataRow
        {
          Values = row.Values.ToDictionary(cell => cell.Key, cell => cell.Value.Clone())
        })
        .ToArray()
    };
  }
}

public sealed class MutableDataRow
{
  public Dictionary<string, AresValue> Values { get; } = [];
}

public static class DataBlockExtensions
{
  public static MutableDataBlock ToMutable(this DataBlock block)
  {
    var mutableBlock = new MutableDataBlock { Name = block.Name };

    mutableBlock.Columns.AddRange(block.Columns);

    foreach(var row in block.Rows)
    {
      var mutableRow = new MutableDataRow();

      foreach(var cell in row.Values)
      {
        mutableRow.Values[cell.Key] = cell.Value.Clone();
      }

      mutableBlock.Rows.Add(mutableRow);
    }

    return mutableBlock;
  }
}
