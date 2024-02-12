using Microsoft.Data.Sqlite;

	ITrackable fat = new FileActivityTracker();
	fat.WriteActivity("The app is running.");
	
	ITrackable sat = new SqliteActivityTracker();    
	sat.WriteActivity("This is from the app!");

public class FileActivityTracker : Trackable, ITrackable
{
	private String FileName;
	
    public FileActivityTracker()
    {
        Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss.fff tt")} : FileActivityTracker ctor...");
    }
	
	public override bool Configure(){
        char pathSlash = Path.DirectorySeparatorChar;

		Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss.fff tt")} : Configure() method called! ");
		// read values from configuration
		FileName = @$"{Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)}{pathSlash}temp{pathSlash}InterfaceStudy.log";
		return true;
	}
	
	public override string StorageTarget
    {
        get
        {
            return FileName;
        }
    }
    
    public override bool WriteActivity(String message)
    {
        try
        {
            File.AppendAllText(StorageTarget, $"{DateTime.Now.ToLongTimeString()}:   {message} {Environment.NewLine}");
            return true;
        }
        catch (Exception ex)
        {

            return false;
        }
    }
}

public interface ITrackable
{
	// StorageTarget is one of the following:
    // 1. full-filename (including path)
    // 2. DB Connection string
    // 3. URI to location where data will be posted.
	
	// STorageTarget will wrap the private var used to contain the filepath,
	// connectionString, URI, etc.
    String StorageTarget { get; }
    bool WriteActivity(String message);
	// The implementation class must call Configure() from its constructor
	// The configure method will read the values from preset configuration.
	bool Configure();
}

public class SqliteActivityTracker: Trackable{
    
    public SqliteCommand Command{
        get{return this.command;}
        set{this.command = value;}
    }
	
	private String connectionString;
	
	private String mainPath;
	private String rootPath;
	private const String tempDir = "temp";
    
    private char pathSlash = Path.DirectorySeparatorChar;
	
    protected SqliteConnection connection;
    protected SqliteCommand command;
	
	public override string StorageTarget
    {
        get
        {
            return connectionString;
        }
    }
	
	public override bool Configure(){
		// read settings from configuration
		rootPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)}";
		// insures this will r
		if (!Directory.Exists(@$"{rootPath}{pathSlash}{tempDir}")){Directory.CreateDirectory(@$"{rootPath}{pathSlash}{tempDir}");}
		mainPath = System.IO.Path.Combine(@$"{rootPath}{pathSlash}{tempDir}","tracker.db");
		Console.WriteLine(mainPath);
		connectionString = $"Data Source={mainPath}";
		connection = new SqliteConnection(StorageTarget);
		command = connection.CreateCommand();
		
		return true;
	}

    public SqliteActivityTracker()
    {
        try{
                // ########### FYI THE DB is created when it is OPENED ########
				
                connection.Open();
                
				File.AppendAllText(@$"{rootPath}{pathSlash}{tempDir}{pathSlash}InterfaceStudy.log", $"{DateTime.Now.ToLongTimeString()}: {Environment.CurrentDirectory} {Environment.NewLine}");
                FileInfo fi = new FileInfo(@$"{rootPath}{pathSlash}{tempDir}{pathSlash}tracker.db");
				
                if (fi.Length == 0){
					connectionString = fi.Name;
                    foreach (String tableCreate in allTableCreation){
                        command.CommandText = tableCreate;
                        command.ExecuteNonQuery();
                    }
                }
                Console.WriteLine(connection.DataSource);
        }
        finally{
            if (connection != null){
                connection.Close();
            }
        }
    }
	
	public override bool WriteActivity(String message)
    {
		Command.CommandText = @"INSERT into Task (Description)values($message);select * from task where id =(SELECT last_insert_rowid())";
        Command.Parameters.AddWithValue("$message",message);

        try{
		    Console.WriteLine("Saving...");
            connection.Open();
            Console.WriteLine("Opened.");
            // id should be last id inserted into table
            var id = Convert.ToInt64(command.ExecuteScalar());
            Console.WriteLine("inserted.");
            return true;
        }
        catch(Exception ex){
            Console.WriteLine($"Error: {ex.Message}");
            return false;
        }
        finally{
            if (connection != null){
                connection.Close();
            }
        }
    }
	
	protected String [] allTableCreation = {
        @"CREATE TABLE Task
                (  [ID] INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                    [Description] NVARCHAR(1000) check(length(Description) <= 1000),
                    [Created] NVARCHAR(30) default (datetime('now','localtime')) check(length(Created) <= 30)
                )"
				};
}

public abstract class Trackable: ITrackable {
	
	public Trackable(){
		Configure();
	}
	
	public abstract bool Configure();
    public abstract bool WriteActivity(String file);
    public abstract string StorageTarget{get;}
}
