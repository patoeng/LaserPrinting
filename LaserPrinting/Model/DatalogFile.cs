using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FirebirdSql.Data.FirebirdClient;
using MesData;
using MesData.LaserMarking;

namespace LaserPrinting.Model
{
    public class DatalogFile
    {
        public Guid Id { get; set; }
        public string FileName { get; set; }
        public int LastRowIndex { get; set; }
        public int LastMarkCount { get; set; }

        public const string Database= "data source=EMBED;user id=SYSDBA;password=meey;initial catalog=DatalogFile.FDB;server type=Embedded;client library=.\\Firebird\\fbclient.dll";
        public const string DbFileName = "DatalogFile.FDB";
        public static List<LaserPrintingProduct> FileParse(ref DatalogFile datalogFile)
        {
            var laserDataConfig = LaserMarkingDataConfig.Load(LaserMarkingDataConfig.FileName);
            laserDataConfig.SaveToFile();

            if (!File.Exists(datalogFile.FileName))
            {
                datalogFile = null;
                return null;
            };
            var text = "";
            using (FileStream fs = new FileStream(datalogFile.FileName, FileMode.Open, FileAccess.Read,
                       FileShare.ReadWrite))
            {
                using (var stream = new StreamReader(fs))
                {
                    text = stream.ReadToEnd();
                }
            }

            var textSplit = text.Split('\n');
            
            var index = -1;
            var data = new List<LaserPrintingProduct>();
            foreach (var stringLine in textSplit)
            {
                index++;
                var valid = LaserMarkingDataConfig.KeywordStrings.Aggregate(true, (current, configKeywordString) => current & stringLine.Contains(configKeywordString));
                if(!valid)continue;

                if (!(index>datalogFile.LastRowIndex)) continue;
                
                var laserDataPoint = LaserMarkingData.ParseData(stringLine, laserDataConfig);
                if (laserDataPoint != null)
                {
                    var laserProduct = new LaserPrintingProduct(laserDataPoint);
                    data.Add(laserProduct);
                }

                datalogFile.LastRowIndex = index;
            }
            return data;
        }
        public static TransactionResult SaveDatalogFileHistory( DatalogFile datalogFile, string databaseFile=DbFileName)
        {
            //Check if database file exist, try create if not exist.
            if (!File.Exists(databaseFile))
            {
                var rslt =  CreateDatabaseFile(databaseFile);
                if (!rslt.Result) return rslt;
            }
            try
            {
                var con = new FbConnection(Database);
                con.Open();


                
                var dataExist =  GetDatalogFileById( datalogFile.Id, databaseFile);
                var commandText = "";

                commandText= dataExist.Data == null ? "INSERT INTO DatalogFiles (Id,FileName,LastRowIndex,LastMarkCount) VALUES (@id,@fileName,@lastRowIndex,@lastMarkCount)" : "UPDATE DatalogFiles SET FileName=@fileName, LastRowIndex=@lastRowIndex,LastMarkCount=@lastMarkCount WHERE id=@id";
                var cmd = new FbCommand(commandText,con);
                var cmdParam = DatalogFileToCmdParameter(datalogFile);
                cmd.Parameters.AddRange(cmdParam);
                 cmd.Prepare();
                var result = cmd.ExecuteNonQuery() ;
                con.Close();
                return TransactionResult.Create(true,result);
            }
            catch (FbException exception)
            {
                return TransactionResult.Create(false, exception,exception.ErrorCode,exception.Message);
            }
            catch (Exception exception)
            {
                return TransactionResult.Create(false, exception, -1, exception.Message);
            }

        }
        public static TransactionResult CreateDatabaseFile(string databaseFile)
        {
            //Check if database file exist, try create if not exist.
            if (File.Exists(databaseFile)) return TransactionResult.Create(true);
            try
            {
                FbConnection.CreateDatabase(Database);

                var con = new FbConnection(Database);
                con.Open();
                
                var cmd = new FbCommand(@"CREATE TABLE DATALOGFILES(ID VARCHAR(38)  PRIMARY KEY, FileName VARCHAR(300), LastRowIndex INTEGER, LastMarkCount INTEGER)", con);
                var result = cmd.ExecuteNonQuery(); 
                con.Close();
                return TransactionResult.Create(true, result);
            }
            catch (FbException exception)
            {
                return TransactionResult.Create(false, exception, exception.ErrorCode, exception.Message);
            }
            catch (Exception exception)
            {
                return TransactionResult.Create(false, exception, -1, exception.Message);
            }
        }
        public static List<FbParameter> DatalogFileToCmdParameter(DatalogFile datalogFile)
        {
            var list = new List<FbParameter>
            {
                new FbParameter("@id",FbDbType.VarChar ,38){Value= datalogFile.Id.ToString()},
                new FbParameter("@fileName",FbDbType.VarChar ,300){Value =datalogFile.FileName },
                new FbParameter("@lastRowIndex",FbDbType.Integer){Value =datalogFile.LastRowIndex },
                new FbParameter("@lastMarkCount",FbDbType.Integer){Value =datalogFile.LastMarkCount}
            };

            return list;
        }

        public static TransactionResult GetDatalogFileByFileName(string datalogFileName, string databaseFile = DbFileName)
        {
            var df = new DatalogFile
            {
                Id = Guid.NewGuid(),
                FileName = datalogFileName
            };

            var path = Path.GetFullPath(databaseFile);
            if (!File.Exists(path)) return TransactionResult.Create(true, df);

            try
            {
                var con = new FbConnection(Database);
                con.Open();

                var cmd = new FbCommand("SELECT * FROM DatalogFiles Where FileName=@fileName", con);
                var cmdParam = DatalogFileToCmdParameter(df);
                cmd.Parameters.AddRange(cmdParam);
                cmd.Prepare();

                var rdr = cmd.ExecuteReader();
                if (rdr.HasRows)
                {
                    if(rdr.Read())
                    {
                        df.Id = Guid.Parse($"{{{rdr.GetString(0)}}}");
                        df.FileName = rdr.GetString(1);
                        df.LastRowIndex = rdr.GetInt32(2);
                        df.LastMarkCount = rdr.GetInt32(3);
                    }
                }
                con.Close();
                return TransactionResult.Create(true, df);
            }
            catch (FbException exception)
            {
                return TransactionResult.Create(false, exception, exception.ErrorCode, exception.Message);
            }
            catch (Exception exception)
            {
                return TransactionResult.Create(false, exception, -1, exception.Message);
            }
        }
        public static TransactionResult GetDatalogFileById(Guid identity, string databaseFile = DbFileName)
        {
           
            DatalogFile df = new DatalogFile
            {
                Id = identity
            };

            var path = Path.GetFullPath(databaseFile);
            if (!File.Exists(path)) return TransactionResult.Create(true, df);

            try
            {

                var con = new FbConnection(Database);
                con.Open();

                var cmd = new FbCommand("SELECT * FROM DatalogFiles Where DatalogFiles.Id=@id", con);
                var cmdParam = DatalogFileToCmdParameter(df);
                cmd.Parameters.AddRange(cmdParam);
                cmd.Prepare();

                var rdr = cmd.ExecuteReader();
                if (rdr.HasRows)
                {
                    if (rdr.Read())
                    {
                        df.Id = Guid.Parse($"{{{rdr.GetString(0)}}}");
                        df.FileName = rdr.GetString(1);
                        df.LastRowIndex = rdr.GetInt32(2);
                        df.LastMarkCount = rdr.GetInt32(3);
                        con.Close();
                        return TransactionResult.Create(true, df);
                    }
                }
                con.Close();
                return TransactionResult.Create(true);
            }
            catch (FbException exception)
            {
                return TransactionResult.Create(false, exception, exception.ErrorCode, exception.Message);
            }
            catch (Exception exception)
            {
                return TransactionResult.Create(false, exception, -1, exception.Message);
            }
        }
    }
}
