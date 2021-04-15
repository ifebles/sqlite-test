using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Setup : MonoBehaviour
{
  private readonly string[] tableTests = new[] {
    "CREATE TABLE IF NOT EXISTS 'users' ("
      + "'id' INTEGER PRIMARY KEY NOT NULL, "
      + "'name' TEXT NOT NULL, "
      + "'creationDate' BIGINT NOT NULL, "
      + "UNIQUE (`name`));",
    "CREATE TABLE IF NOT EXISTS 'userDetails' ("
      + "'id' INTEGER PRIMARY KEY NOT NULL, "
      + "'lastClick' BIGINT NOT NULL, "
      + "'userId' INTEGER NOT NULL, "
      + "FOREIGN KEY ('userId') REFERENCES 'users'('id'));",
  };

  private Color[] colorPalette;
  private Utilities.SQLiteSetup sqliteManager;
  private Vector2 size;
  private Vector3 elemPos;
  private SpriteRenderer spriteRenderer;
  private int colorPointer = 0;

  private Color currentColor
  {
    get => spriteRenderer.color;
    set
    {
      spriteRenderer.color = value;

      var index = colorPointer % colorPalette.Length;
      var iteration = (int)(colorPointer / colorPalette.Length);

      switch (index)
      {
        case 0:
          var usersCountResult = sqliteManager.GetRawCollection($"SELECT COUNT(*) FROM `users`;");
          var detailsCountResult = sqliteManager.GetRawCollection($"SELECT COUNT(*) FROM `userDetails`;");

          if (usersCountResult == null || usersCountResult.Length == 0 || usersCountResult[0].Length == 0)
          {
            Debug.LogWarning($"No results from the users' count");
            break;
          }

          if (detailsCountResult == null || detailsCountResult.Length == 0 || detailsCountResult[0].Length == 0)
          {
            Debug.LogWarning($"No results from the users' count");
            break;
          }

          Debug.Log($"Count results: users=`{usersCountResult[0][0]}`; userDetails=`{detailsCountResult[0][0]}`");
          break;

        case 1:
          var userInsertResult = sqliteManager.Execute("INSERT INTO `users` (`name`, `creationDate`) "
            + $"VALUES ('test{iteration}', {DateTime.UtcNow.Ticks});");

          Debug.Log("Insert users result: " + userInsertResult);
          break;

        case 2:
          var idResultForInsert = sqliteManager.GetRawCollection($"SELECT `id` FROM `users` WHERE `name` = 'test{iteration}';");

          if (idResultForInsert == null || idResultForInsert.Length == 0 || idResultForInsert[0].Length == 0)
          {
            Debug.LogWarning($"No results from the users table with `test{iteration}`");
            break;
          }

          var detailsInsertResult = sqliteManager.Execute("INSERT INTO `userDetails` (`lastClick`, `userId`) "
            + $"VALUES ({DateTime.UtcNow.Ticks}, {idResultForInsert[0][0]});");

          Debug.Log("Insert userDetails result: " + detailsInsertResult);
          break;

        case 3:
          var idResultForUpdate = sqliteManager.GetRawCollection($"SELECT `id` FROM `users` WHERE `name` = 'test{iteration}';");

          if (idResultForUpdate == null || idResultForUpdate.Length == 0 || idResultForUpdate[0].Length == 0)
          {
            Debug.LogWarning($"No results from the users table with `test{iteration}`");
            break;
          }

          var detailsUpdateResult = sqliteManager.Execute($"UPDATE `userDetails` SET lastClick={DateTime.UtcNow.Ticks} "
            + $"WHERE `userId`={idResultForUpdate[0][0]};");

          Debug.Log("Update userDetails result: " + detailsUpdateResult);
          break;
      }
    }
  }

  void Awake()
  {
    sqliteManager = new Utilities.SQLiteSetup();

    Debug.Log("Initializing table creation...");

    foreach (var tableScript in this.tableTests)
      sqliteManager.Execute(tableScript);

    Debug.Log("Finished executing scripts for tables");

    spriteRenderer = GetComponent<SpriteRenderer>();
    size = Camera.main.ViewportToWorldPoint(spriteRenderer.bounds.size);
    elemPos = Camera.main.WorldToScreenPoint(transform.position);

    colorPalette = new[] {
      spriteRenderer.color,
      Color.green,
      Color.blue,
      Color.yellow,
    };
  }

  // Update is called once per frame
  void Update()
  {
    if (!Input.GetMouseButtonUp(0))
      return;

    var mousePos = Input.mousePosition;
    var maxBounds = Camera.main.WorldToScreenPoint(spriteRenderer.bounds.max);
    var minBounds = Camera.main.WorldToScreenPoint(spriteRenderer.bounds.min);

    if (!(mousePos.x >= minBounds.x && mousePos.x <= maxBounds.x
      && mousePos.y >= minBounds.y && mousePos.y <= maxBounds.y))
    {
      var detailsRsult = sqliteManager.Execute("DELETE FROM `userDetails`;");
      var usersResult = sqliteManager.Execute("DELETE FROM `users`;");
      Debug.Log($"Cleared DB contents: users=`{usersResult}`; userDetails=`{detailsRsult}`");
      return;
    }

    this.currentColor = colorPalette[(++colorPointer) % colorPalette.Length];
  }
}
