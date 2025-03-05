using Beamable.Server;

namespace Beamable.Server
{
	/// <summary>
	/// This class represents the existence of the Database database.
	/// Use it for type safe access to the database.
	/// <code>
	/// var db = await Storage.GetDatabase&lt;Database&gt;();
	/// </code>
	/// </summary>
	[StorageObject("Database")]
	public class Database : MongoStorageObject
	{
		
	}
}
