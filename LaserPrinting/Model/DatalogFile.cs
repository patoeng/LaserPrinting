using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using System.Data.SQLite;

namespace LaserPrinting.Model
{
    public class DatalogFile
    {
        public Guid Id { get; set; }
        public string FileName { get; set; }
        public int LastRowIndex { get; set; }
        public int LastMarkCount { get; set; }


        public static List<LaserPrintingProduct> FileParse(ref DatalogFile datalogFile)
        {
            if (!File.Exists(datalogFile.FileName))
            {
                datalogFile = null;
                return null;
            };

            var text = File.ReadAllLines(datalogFile.FileName);
            var index = 0;
            var data = new List<LaserPrintingProduct>();
            var markCount = 0;
            foreach (var str in text)
            {
                if (string.IsNullOrEmpty(str))
                {
                    index++;
                    continue;
                }
                if (str.Contains("the MarkCode:"))// mark indicator, get barcode, date start, and mark count
                {
                    //get date
                    DateTime dt;
                    var dateString = str.Substring(0, 23);
                    var tryDate = DateTime.TryParseExact(dateString, "yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture, DateTimeStyles.None, out dt);
                    if (!tryDate) continue;

                    //get barcode
                    var barcodeString = str.Substring(str.IndexOf("Code:") + 5, 19);

                    //get Mark Count
                    //var markCount = str.Substring(str.IndexOf("Count:") + 6, str.IndexOf(",Marking") - str.IndexOf("Count:") - 6);

                    var product = new LaserPrintingProduct
                    {
                        DatalogFileId = datalogFile.Id,
                        Barcode = barcodeString,
                        PrintedStartDateTime = dt,
                        PrintedEndDateTime = dt,
                        //MarkCount = Convert.ToInt32(markCount)
                    };
                    markCount++;
                    product.MarkCount = markCount;

                    if (product.MarkCount > datalogFile.LastMarkCount)
                    {
                        data.Add(product);
                        datalogFile.LastMarkCount = product.MarkCount;
                    }
                }

                datalogFile.LastRowIndex = index;

                index++;

            }
            return data;
        }
        public static bool SaveDatalogFileHistory(string databaseFile, DatalogFile datalogFile)
        {
            //Check if database file exist, try create if not exist.
            if (!File.Exists(databaseFile))
            {
                var rslt = DatalogFile.CreateDatabaseFile(databaseFile);
                if (!rslt) return false;
            }
            try
            {
                var path = Path.GetFullPath(databaseFile);

                var con = new SQLiteConnection($"Data Source={path};Version=3;");
                con.Open();
                //check if data exist 

               

                var cmd = new SQLiteCommand(con);
                var dataExist = GetDatalogFileById(databaseFile, datalogFile.Id);

                if (dataExist == null) //insert new if null
                {
                    cmd.CommandText = "INSERT INTO DatalogFiles (Id,FileName,LastRowIndex,LastMarkCount) VALUES(@id,@fileName,@lastRowIndex,@lastMarkCount)";
                }
                else //update
                {
                    cmd.CommandText = "UPDATE DatalogFiles SET FileName=@fileName, LastRowIndex=@lastRowIndex,LastMarkCount=@lastMarkCount WHERE id=@id";
                }

                var cmdParam = DatalogFileToCmdParameter(datalogFile);
                cmd.Parameters.AddRange(cmdParam.ToArray());
                cmd.Prepare();
                cmd.ExecuteNonQuery();

                con.Close();
                return true;
            }
            catch(Exception exception)
            {
                return false;
            }
            
        }
        public static bool CreateDatabaseFile(string databaseFile)
        {
            //Check if database file exist, try create if not exist.
            if (File.Exists(databaseFile)) return true;


            try
            {
                var path = Path.GetFullPath(databaseFile);
                SQLiteConnection.CreateFile(path);


                var con = new SQLiteConnection($"Data Source={path};Version=3;");

                con.Open();

                var cmd = new SQLiteCommand(con);

                cmd.CommandText = "DROP TABLE IF EXISTS DatalogFiles";
                cmd.ExecuteNonQuery();

                cmd.CommandText = @"CREATE TABLE DatalogFiles(id string PRIMARY KEY,FileName TEXT, LastRowIndex INT, LastMarkCount INT)";
                cmd.ExecuteNonQuery();
                con.Close();
                return true;
            }
            catch
            {
                return false;
            }
        }
        public static List<SQLiteParameter> DatalogFileToCmdParameter(DatalogFile datalogFile)
        {
            var list = new List<SQLiteParameter>();
            list.Add(new SQLiteParameter("@id",datalogFile.Id.ToString()));
            list.Add(new SQLiteParameter("@fileName", datalogFile.FileName));
            list.Add(new SQLiteParameter("@lastRowIndex", datalogFile.LastRowIndex));
            list.Add(new SQLiteParameter("@lastMarkCount", datalogFile.LastMarkCount));

            return list;
        }

        public static DatalogFile GetDatalogFileByFileName(string databaseFile, string datalogFileName)
        {
            DatalogFile df = new DatalogFile();
            df.Id = Guid.NewGuid();
            df.FileName = datalogFileName;

            var path = Path.GetFullPath(databaseFile);
            if (!File.Exists(path)) return df;

            try
            {
               

                var con = new SQLiteConnection($"Data Source={path};Version=3;");
                con.Open();

                var cmd = new SQLiteCommand(con);
                cmd.CommandText = "SELECT * FROM DatalogFiles Where FileName=@fileName";
                var cmdParam = DatalogFileToCmdParameter(df);
                cmd.Parameters.AddRange(cmdParam.ToArray());
                cmd.Prepare();

                var rdr = cmd.ExecuteReader();
                if (rdr.HasRows)
                {
                    rdr.Read();


                    df.Id = rdr.GetGuid(0);
                    df.FileName = rdr.GetString(1);
                    df.LastRowIndex = rdr.GetInt32(2);
                    df.LastMarkCount = rdr.GetInt32(3);
                }
                con.Close();
            }
            catch
            {
               
            }
            return df;
        }
        public static DatalogFile GetDatalogFileById(string databaseFile, Guid identity)
        {
            DatalogFile df = new DatalogFile();
            df.Id = identity;

            var path = Path.GetFullPath(databaseFile);
            if (!File.Exists(path)) return df;

            try
            {


                var con = new SQLiteConnection($"Data Source={path};Version=3;");
                con.Open();

                var cmd = new SQLiteCommand(con);
                cmd.CommandText = "SELECT * FROM DatalogFiles Where Id=@id";
                var cmdParam = DatalogFileToCmdParameter(df);
                cmd.Parameters.AddRange(cmdParam.ToArray());
                cmd.Prepare();

                var rdr = cmd.ExecuteReader();
                if (rdr.HasRows)
                {
                    rdr.Read();


                    df.Id = rdr.GetGuid(0);
                    df.FileName = rdr.GetString(1);
                    df.LastRowIndex = rdr.GetInt32(2);
                    df.LastMarkCount = rdr.GetInt32(3);
                    con.Close();
                    return df;
                }
                con.Close();
            }
            catch
            {
                
            }
            return null;
        }
    }
}
