﻿using System;
using System.Data.Common;
using System.Data;
using System.Data.SQLite;

namespace test
{

  /// <summary>
  /// Scalar user-defined function.  In this example, the same class is declared twice with 
  /// different function names to demonstrate how to use alias names for user-defined functions.
  /// </summary>
  [SQLiteFunction(Name = "Foo", Arguments = 2, FuncType = FunctionType.Scalar)]
  [SQLiteFunction(Name = "TestFunc", Arguments = 2, FuncType = FunctionType.Scalar)]
  class TestFunc : SQLiteFunction
  {
    public override object Invoke(object[] args)
    {
      if (args[0].GetType() != typeof(int)) return args[0];

      int Param1 = Convert.ToInt32(args[0]); // First parameter
      int Param2 = Convert.ToInt32(args[1]); // Second parameter

      return Param1 + Param2;
    }
  }

  /// <summary>
  /// Aggregate user-defined function.  Arguments = -1 means any number of arguments is acceptable
  /// </summary>
  [SQLiteFunction(Name = "MyCount", Arguments = -1, FuncType = FunctionType.Aggregate)]
  class MyCount : SQLiteFunction
  {
    public override void Step(object[] args, int nStep, ref object contextData)
    {
      if (contextData == null)
      {
        contextData = 1;
      }
      else
        contextData = (int)contextData + 1;
    }

    public override object Final(object contextData)
    {
      return contextData;
    }
  }

  /// <summary>
  /// User-defined collating sequence.
  /// </summary>
  [SQLiteFunction(Name = "MYSEQUENCE", FuncType = FunctionType.Collation)]
  class MySequence : SQLiteFunction
  {
    public override int Compare(string param1, string param2)
    {
      // Make sure the string "Field3" is sorted out of order
      if (param1 == "Field3") return 1;
      if (param2 == "Field3") return -1;
      return String.Compare(param1, param2, true);
    }
  }

  internal class TestCases
  {
    internal Form1 frm;

    internal void Run(DbConnection cnn)
    {
      frm = new Form1();

      frm.Show();

      frm.WriteLine("\r\nBeginning Test on " + cnn.GetType().ToString());
      try { CreateTable(cnn); frm.WriteLine("SUCCESS - CreateTable"); }
      catch (Exception) { frm.WriteLine("FAIL - CreateTable"); }

      try { InsertTable(cnn); frm.WriteLine("SUCCESS - InsertTable"); }
      catch (Exception) { frm.WriteLine("FAIL - InsertTable"); }

      try { VerifyInsert(cnn); frm.WriteLine("SUCCESS - VerifyInsert"); }
      catch (Exception) { frm.WriteLine("FAIL - VerifyInsert"); }

      try { CoersionTest(cnn); frm.WriteLine("FAIL - CoersionTest"); }
      catch (Exception) { frm.WriteLine("SUCCESS - CoersionTest"); }

      try { ParameterizedInsert(cnn); frm.WriteLine("SUCCESS - ParameterizedInsert"); }
      catch (Exception) { frm.WriteLine("FAIL - ParameterizedInsert"); }

      try { BinaryInsert(cnn); frm.WriteLine("SUCCESS - BinaryInsert"); }
      catch (Exception) { frm.WriteLine("FAIL - BinaryInsert"); }

      try { VerifyBinaryData(cnn); frm.WriteLine("SUCCESS - VerifyBinaryData"); }
      catch (Exception) { frm.WriteLine("FAIL - VerifyBinaryData"); }

      try { ParameterizedInsertMissingParams(cnn); frm.WriteLine("FAIL - ParameterizedInsertMissingParams"); }
      catch (Exception) { frm.WriteLine("SUCCESS - ParameterizedInsertMissingParams"); }

      try { InsertMany(cnn, false); frm.WriteLine("SUCCESS - InsertMany"); }
      catch (Exception) { frm.WriteLine("FAIL - InsertMany"); }

      try { InsertMany(cnn, true); frm.WriteLine("SUCCESS - InsertManyWithIdentityFetch"); }
      catch (Exception) { frm.WriteLine("FAIL - InsertManyWithIdentityFetch"); }

      try { FastInsertMany(cnn); frm.WriteLine("SUCCESS - FastInsertMany"); }
      catch (Exception) { frm.WriteLine("FAIL - FastInsertMany"); }

      try { IterationTest(cnn); frm.WriteLine("SUCCESS - Iteration Test"); }
      catch (Exception) { frm.WriteLine("FAIL - Iteration Test"); }

      try { UserFunction(cnn); frm.WriteLine("SUCCESS - UserFunction"); }
      catch (Exception) { frm.WriteLine("FAIL - UserFunction"); }

      try { UserAggregate(cnn); frm.WriteLine("SUCCESS - UserAggregate"); }
      catch (Exception) { frm.WriteLine("FAIL - UserAggregate"); }

      try { UserCollation(cnn); frm.WriteLine("SUCCESS - UserCollation"); }
      catch (Exception) { frm.WriteLine("FAIL - UserCollation"); }

      try { DropTable(cnn); frm.WriteLine("SUCCESS - DropTable"); }
      catch (Exception) { frm.WriteLine("FAIL - DropTable"); }

      frm.WriteLine("\r\nTests Finished.");
    }

    internal void CreateTable(DbConnection cnn)
    {
      using (DbCommand cmd = cnn.CreateCommand())
      {
        cmd.CommandText = "CREATE TABLE TestCase (ID integer primary key autoincrement, Field1 Integer, Field2 Float, Field3 VARCHAR(50), Field4 CHAR(10), Field5 DateTime, Field6 Image)";
        //cmd.CommandText = "CREATE TABLE TestCase (ID bigint primary key identity, Field1 Integer, Field2 Float, Field3 VARCHAR(50), Field4 CHAR(10), Field5 DateTime, Field6 Image)";
        cmd.ExecuteNonQuery();
      }
    }

    internal void DropTable(DbConnection cnn)
    {
      using (DbCommand cmd = cnn.CreateCommand())
      {
        cmd.CommandText = "DROP TABLE TestCase";
        cmd.ExecuteNonQuery();
      }
    }

    internal void InsertTable(DbConnection cnn)
    {
      using (DbCommand cmd = cnn.CreateCommand())
      {
        cmd.CommandText = "INSERT INTO TestCase(Field1, Field2, Field3, Field4, Field5) VALUES(1, 3.14159, 'Field3', 'Field4', '2005-01-01 13:49:00')";
        cmd.ExecuteNonQuery();
      }
    }

    internal void VerifyInsert(DbConnection cnn)
    {
      using (DbCommand cmd = cnn.CreateCommand())
      {
        cmd.CommandText = "SELECT Field1, Field2, Field3, Field4, Field5 FROM TestCase";
        cmd.Prepare();
        using (DbDataReader rd = cmd.ExecuteReader())
        {
          if (rd.Read())
          {
            long Field1 = rd.GetInt64(0);
            double Field2 = rd.GetDouble(1);
            string Field3 = rd.GetString(2);
            string Field4 = rd.GetString(3).TrimEnd();
            DateTime Field5 = rd.GetDateTime(4);

            if (Field1 != 1) throw new ArgumentOutOfRangeException("Non-Match on Field1");
            if (Field2 != 3.14159) throw new ArgumentOutOfRangeException("Non-Match on Field2");
            if (Field3 != "Field3") throw new ArgumentOutOfRangeException("Non-Match on Field3");
            if (Field4 != "Field4") throw new ArgumentOutOfRangeException("Non-Match on Field4");
            if (Field5.CompareTo(DateTime.Parse("2005-01-01 13:49:00")) != 0) throw new ArgumentOutOfRangeException("Non-Match on Field5");
          }
          else throw new ArgumentOutOfRangeException("No data in table");
        }
      }
    }

    internal void CoersionTest(DbConnection cnn)
    {
      using (DbCommand cmd = cnn.CreateCommand())
      {
        cmd.CommandText = "SELECT Field1, Field2, Field3, Field4, Field5, 'A', 1, 1 + 1, 3.14159 FROM TestCase";
        using (DbDataReader rd = cmd.ExecuteReader())
        {
          if (rd.Read())
          {
            object Field1 = rd.GetInt32(0);
            object Field2 = rd.GetDouble(1);
            object Field3 = rd.GetString(2);
            object Field4 = rd.GetString(3).TrimEnd();
            object Field5 = rd.GetDateTime(4);

            // The next statement should cause an exception
            Field1 = rd.GetString(0);
            Field2 = rd.GetString(1);
            Field3 = rd.GetString(2);
            Field4 = rd.GetString(3);
            Field5 = rd.GetString(4);

            Field1 = rd.GetInt32(0);
            Field2 = rd.GetInt32(1);
            Field3 = rd.GetInt32(2);
            Field4 = rd.GetInt32(3);
            Field5 = rd.GetInt32(4);

            Field1 = rd.GetDecimal(0);
            Field2 = rd.GetDecimal(1);
            Field3 = rd.GetDecimal(2);
            Field4 = rd.GetDecimal(3);
            Field5 = rd.GetDecimal(4);
          }
          else throw new ArgumentOutOfRangeException("No data in table");
        }
      }
    }

    internal void ParameterizedInsert(DbConnection cnn)
    {
      using (DbCommand cmd = cnn.CreateCommand())
      {
        cmd.CommandText = "INSERT INTO TestCase(Field1, Field2, Field3, Field4, Field5) VALUES(?,?,?,?,?)";
        DbParameter Field1 = cmd.CreateParameter();
        DbParameter Field2 = cmd.CreateParameter();
        DbParameter Field3 = cmd.CreateParameter();
        DbParameter Field4 = cmd.CreateParameter();
        DbParameter Field5 = cmd.CreateParameter();

        Field1.Value = 2;
        Field2.Value = 3.14159;
        Field3.Value = "Param Field3";
        Field4.Value = "Field4 Par";
        Field5.Value = DateTime.Now;

        cmd.Parameters.Add(Field1);
        cmd.Parameters.Add(Field2);
        cmd.Parameters.Add(Field3);
        cmd.Parameters.Add(Field4);
        cmd.Parameters.Add(Field5);

        cmd.ExecuteNonQuery();
      }
    }

    internal void BinaryInsert(DbConnection cnn)
    {
      using (DbCommand cmd = cnn.CreateCommand())
      {
        cmd.CommandText = "INSERT INTO TestCase(Field6) VALUES(?)";
        DbParameter Field6 = cmd.CreateParameter();

        byte[] b = new byte[4000];
        b[0] = 1;
        b[100] = 2;
        b[1000] = 3;
        b[2000] = 4;
        b[3000] = 5;

        Field6.Value = b;

        cmd.Parameters.Add(Field6);

        cmd.ExecuteNonQuery();
      }
    }

    internal void VerifyBinaryData(DbConnection cnn)
    {
      using (DbCommand cmd = cnn.CreateCommand())
      {
        cmd.CommandText = "SELECT Field6 FROM TestCase WHERE Field6 IS NOT NULL";
        byte[] b = new byte[4000];

        using (DbDataReader rd = cmd.ExecuteReader())
        {
          if (rd.Read() == false) throw new ArgumentOutOfRangeException();

          rd.GetBytes(0, 0, b, 0, 4000);

          if (b[0] != 1) throw new ArgumentException();
          if (b[100] != 2) throw new ArgumentException();
          if (b[1000] != 3) throw new ArgumentException();
          if (b[2000] != 4) throw new ArgumentException();
          if (b[3000] != 5) throw new ArgumentException();
        }
      }
    }

    internal void ParameterizedInsertMissingParams(DbConnection cnn)
    {
      using (DbCommand cmd = cnn.CreateCommand())
      {
        cmd.CommandText = "INSERT INTO TestCase(Field1, Field2, Field3, Field4, Field5) VALUES(?,?,?,?,?)";
        DbParameter Field1 = cmd.CreateParameter();
        DbParameter Field2 = cmd.CreateParameter();
        DbParameter Field3 = cmd.CreateParameter();
        DbParameter Field4 = cmd.CreateParameter();
        DbParameter Field5 = cmd.CreateParameter();

        Field1.DbType = System.Data.DbType.Int32;

        Field1.Value = 2;
        Field2.Value = 3.14159;
        Field3.Value = "Field3 Param";
        Field4.Value = "Field4 Par";
        Field5.Value = DateTime.Now;

        cmd.Parameters.Add(Field1);
        cmd.Parameters.Add(Field2);
        cmd.Parameters.Add(Field3);
        cmd.Parameters.Add(Field4);

        // Assertion here, not enough parameters
        cmd.ExecuteNonQuery();
      }
    }

    // Utilizes the SQLiteCommandBuilder, which in turn utilizes SQLiteDataReader's GetSchemaTable() functionality
    internal void InsertMany(DbConnection cnn, bool bWithIdentity)
    {
      int nmax = 1000;

      using (DbTransaction dbTrans = cnn.BeginTransaction())
      {
        using (DbDataAdapter adp = new SQLiteDataAdapter())
        {
          using (DbCommand cmd = cnn.CreateCommand())
          {
            cmd.Transaction = dbTrans;
            cmd.CommandText = "SELECT * FROM TestCase WHERE 1=2";
            adp.SelectCommand = cmd;

            using (DbCommandBuilder bld = new SQLiteCommandBuilder())
            {
              bld.DataAdapter = adp;
              adp.InsertCommand = bld.GetInsertCommand();

              if (bWithIdentity)
              {
                adp.InsertCommand.CommandText += ";SELECT [ID] FROM TestCase WHERE RowID = last_insert_rowid()";
                adp.InsertCommand.UpdatedRowSource = UpdateRowSource.FirstReturnedRecord;
              }

              using (DataTable tbl = new DataTable())
              {
                adp.Fill(tbl);
                for (int n = 0; n < nmax; n++)
                {
                  DataRow row = tbl.NewRow();
                  row[1] = n + nmax;
                  tbl.Rows.Add(row);
                }

                frm.Write(String.Format("          InsertMany{0} ({1} rows) Begins ... ", (bWithIdentity == true) ? "WithIdentityFetch":"                 ", nmax));
                long dtStart = DateTime.Now.Ticks;
                adp.Update(tbl);
                long dtEnd = DateTime.Now.Ticks;
                dtEnd -= dtStart;
                frm.Write(String.Format("Ends in {0} ms ... ", (dtEnd / 10000)));

                dtStart = DateTime.Now.Ticks;
                dbTrans.Commit();
                dtEnd = DateTime.Now.Ticks;
                dtEnd -= dtStart;
                frm.WriteLine(String.Format("Commits in {0} ms", (dtEnd / 10000)));
              }
            }
          }
        }
      }
    }

    internal void FastInsertMany(DbConnection cnn)
    {
      using (DbTransaction dbTrans = cnn.BeginTransaction())
      {
        long dtStart;
        long dtEnd;

        using (DbCommand cmd = cnn.CreateCommand())
        {
          cmd.CommandText = "INSERT INTO TestCase(Field1) VALUES(?)";
          DbParameter Field1 = cmd.CreateParameter();

          cmd.Parameters.Add(Field1);

          frm.WriteLine(String.Format("          Fast insert using parameters and prepared statement\r\n          -> (10,000 rows) Begins ... "));
          dtStart = DateTime.Now.Ticks;
          for (int n = 0; n < 10000; n++)
          {
            Field1.Value = n + 100000;
            cmd.ExecuteNonQuery();
          }

          dtEnd = DateTime.Now.Ticks;
          dtEnd -= dtStart;
          frm.Write(String.Format("          -> Ends in {0} ms ... ", (dtEnd / 10000)));
        }

        dtStart = DateTime.Now.Ticks;
        dbTrans.Rollback();
        dtEnd = DateTime.Now.Ticks;
        dtEnd -= dtStart;
        frm.WriteLine(String.Format("Rolled back in {0} ms", (dtEnd / 10000)));
      }
    }

    // Causes the user-defined function to be called
    internal void UserFunction(DbConnection cnn)
    {
      using (DbCommand cmd = cnn.CreateCommand())
      {
        int nTimes;
        long dtStart;

        nTimes = 0;
        cmd.CommandText = "SELECT Foo('ee','foo')";
        dtStart = DateTime.Now.Ticks;
        while (DateTime.Now.Ticks - dtStart < 10000000)
        {
          cmd.ExecuteNonQuery();
          nTimes++;
        }
        frm.WriteLine(String.Format("          User (text)  command executed {0} times in 1 second.", nTimes));

        nTimes = 0;
        cmd.CommandText = "SELECT Foo(10,11)";
        dtStart = DateTime.Now.Ticks;
        while (DateTime.Now.Ticks - dtStart < 10000000)
        {
          cmd.ExecuteNonQuery();
          nTimes++;
        }
        frm.WriteLine(String.Format("          UserFunction command executed {0} times in 1 second.", nTimes));

        nTimes = 0;
        cmd.CommandText = "SELECT ABS(1)";
        dtStart = DateTime.Now.Ticks;
        while (DateTime.Now.Ticks - dtStart < 10000000)
        {
          cmd.ExecuteNonQuery();
          nTimes++;
        }
        frm.WriteLine(String.Format("          Intrinsic    command executed {0} times in 1 second.", nTimes));

        nTimes = 0;
        cmd.CommandText = "SELECT lower('FOO')";
        dtStart = DateTime.Now.Ticks;
        while (DateTime.Now.Ticks - dtStart < 10000000)
        {
          cmd.ExecuteNonQuery();
          nTimes++;
        }
        frm.WriteLine(String.Format("          Intrin (txt) command executed {0} times in 1 second.", nTimes));

        nTimes = 0;
        cmd.CommandText = "SELECT 1";
        dtStart = DateTime.Now.Ticks;
        while (DateTime.Now.Ticks - dtStart < 10000000)
        {
          cmd.ExecuteNonQuery();
          nTimes++;
        }
        frm.WriteLine(String.Format("          Raw Value    command executed {0} times in 1 second.", nTimes));
      }
    }

    internal void IterationTest(DbConnection cnn)
    {
      using (DbCommand cmd = cnn.CreateCommand())
      {
        long dtStart;
        long dtEnd;
        int nCount;
        long n;

        cmd.CommandText = "SELECT Foo(ID, ID) FROM TestCase";
        cmd.Prepare();
        dtStart = DateTime.Now.Ticks;
        nCount = 0;
        using (DbDataReader rd = cmd.ExecuteReader())
        {
          while (rd.Read())
          {
            n = rd.GetInt64(0);
            nCount++;
          }
          dtEnd = DateTime.Now.Ticks;
        }
        frm.WriteLine(String.Format("          User Function iteration of {0} records in {1} ms", nCount, (dtEnd - dtStart) / 10000));

        cmd.CommandText = "SELECT ID FROM TestCase";
        cmd.Prepare();
        dtStart = DateTime.Now.Ticks;
        nCount = 0;
        using (DbDataReader rd = cmd.ExecuteReader())
        {
          while (rd.Read())
          {
            n = rd.GetInt64(0);
            nCount++;
          }
          dtEnd = DateTime.Now.Ticks;
        }
        frm.WriteLine(String.Format("          Raw iteration of {0} records in {1} ms", nCount, (dtEnd - dtStart) / 10000));

        cmd.CommandText = "SELECT ABS(ID) FROM TestCase";
        cmd.Prepare();
        dtStart = DateTime.Now.Ticks;
        nCount = 0;
        using (DbDataReader rd = cmd.ExecuteReader())
        {
          while (rd.Read())
          {
            n = rd.GetInt64(0);
            nCount++;
          }
          dtEnd = DateTime.Now.Ticks;
        }
        frm.WriteLine(String.Format("          Intrinsic Function iteration of {0} records in {1} ms", nCount, (dtEnd - dtStart) / 10000));

      }
    }

    // Causes the user-defined aggregate to be iterated through
    internal void UserAggregate(DbConnection cnn)
    {
      using (DbCommand cmd = cnn.CreateCommand())
      {
        long dtStart;
        int n = 0;
        int nCount;

        cmd.CommandText = "SELECT MyCount(*) FROM TestCase";

        nCount = 0;
        dtStart = DateTime.Now.Ticks;
        while (DateTime.Now.Ticks - dtStart < 10000000)
        {
          n = Convert.ToInt32(cmd.ExecuteScalar());
          nCount++;
        }
        if (n != 2003) throw new ArgumentOutOfRangeException("Unexpected count");
        frm.WriteLine(String.Format("          UserAggregate executed {0} times in 1 second.", nCount));
      }
    }

    // Causes the user-defined collation sequence to be iterated through
    internal void UserCollation(DbConnection cnn)
    {
      using (DbCommand cmd = cnn.CreateCommand())
      {
        // Using a default collating sequence in descending order, "Param Field3" will appear at the top
        // and "Field3" will be next, followed by a NULL.  Our user-defined collating sequence will 
        // deliberately place them out of order so Field3 is first.
        cmd.CommandText = "SELECT Field3 FROM TestCase ORDER BY Field3 COLLATE MYSEQUENCE DESC";
        string s = (string)cmd.ExecuteScalar();
        if (s != "Field3") throw new ArgumentOutOfRangeException("MySequence didn't sort properly");
      }
    }
  }
}