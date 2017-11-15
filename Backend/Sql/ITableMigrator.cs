﻿namespace HD
{
  public interface ITableMigrator
  {
    string tableName
    {
      get;
    }

    long currentVersion
    {
      get;
    }

    /// <summary>
    /// Returns the Sql command to run, or null.
    /// </summary>
    string UpgradeTo(long version);
  }
}