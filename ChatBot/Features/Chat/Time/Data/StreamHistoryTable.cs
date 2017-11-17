﻿using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;

namespace HD
{
  public class StreamHistoryTable : ITableMigrator
  {
    #region Data
    public static StreamHistoryTable instance;

    const string stateField = "State";
    const string timeField = "TimeInTicks";

    long ITableMigrator.currentVersion
    {
      get
      {
        return 0;
      }
    }

    public string tableName
    {
      get
      {
        return "StreamHistory";
      }
    }
    #endregion

    #region Properties
    public bool isLive
    {
      get
      {
        return GetLastState() == HistoryState.Live;
      }
    }
    #endregion

    #region Init
    public StreamHistoryTable()
    {
      Debug.Assert(instance == null);

      instance = this;
    }

    string ITableMigrator.UpgradeTo(
      long version)
    {
      switch (version)
      {
        case 0:
          return $@"
CREATE TABLE IF NOT EXISTS `{tableName}` ( 
  `TimeInTicks` INTEGER NOT NULL, 
  `State` INTEGER NOT NULL );
          ";
        default:
          return null;
      }
    }
    #endregion

    #region Write
    /// <summary>
    /// Returns true if history was recorded (ignored if state did not change).
    /// </summary>
    public bool AddStreamHistory(
      HistoryState historyState)
    {
      if (GetLastState() == historyState)
      {
        return false;
      }

      SqlManager.ExecuteNonQuery($@"
INSERT INTO {tableName} ({timeField}, {stateField}) 
VALUES(@{timeField}, @{stateField})
        ",
        ($"@{timeField}", DateTime.Now.Ticks),
        ($"@{stateField}", (long)historyState));

      return true;
    }
    #endregion

    #region Read
    public TimeSpan? GetUptime()
    {
      if(isLive == false)
      { // Offline
        return null;
      }

      string sql = $@"
SELECT {timeField}
FROM {tableName}
ORDER BY {timeField} DESC
        ";

      object result = SqlManager.GetScalar(sql);

      if (result == null)
      { // No data available yet
        return null;
      }

      DateTime streamStartTime = new DateTime((long)result);
      return DateTime.Now - streamStartTime;
    }

    public HistoryState GetLastState()
    {
      string sql = $@"
SELECT {stateField}
FROM {tableName}
ORDER BY {timeField} DESC
        ";

      object result = SqlManager.GetScalar(sql);

      if (result == null)
      {
        return HistoryState.Offline;
      }

      return (HistoryState)(long)result;
    }

    /// <summary>
    /// TODO - how to deal with streaming at midnight?
    /// </summary>
    public TimeSpan GetPreviousUptimeSince(
      DateTime afterThisTime)
    {
      TimeSpan uptime = new TimeSpan();

      string sql = $@"
SELECT * 
FROM {tableName} 
WHERE {timeField} > @{timeField}
ORDER BY {timeField} Desc
        ";

      List<DateTime> eventTimeList = new List<DateTime>();
      HistoryState lastState = HistoryState.Offline;
      using (DbDataReader reader = SqlManager.GetReader(sql, ($"@{timeField}", afterThisTime.Ticks)))
      {
        while (reader.Read())
        {
          HistoryState state = (HistoryState)(long)reader[stateField];
          if (state == lastState)
          { // Skip rows with the same state as the one before it.
            continue;
          }
          lastState = state;
          DateTime dateTime = new DateTime((long)reader[timeField]);
          eventTimeList.Add(dateTime);
        }
      }

      // This gives us oldest event first
      eventTimeList.Reverse();

      // Even is online, odd is offline
      for (int i = 0; i < eventTimeList.Count - 1; i += 2)
      {
        TimeSpan deltaTime = eventTimeList[i + 1] - eventTimeList[i];
        Debug.Assert(deltaTime > TimeSpan.Zero);

        uptime += deltaTime;
      }

      return uptime;
    }

    public TimeSpan GetPreviousUptimeToday()
    {
      return GetPreviousUptimeSince(DateTime.Now.Date);
    }
    #endregion
  }
}