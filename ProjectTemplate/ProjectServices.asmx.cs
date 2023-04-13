using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Services;
using MySql.Data;
using MySql.Data.MySqlClient;
using System.Data;

namespace ProjectTemplate
{
	[WebService(Namespace = "http://tempuri.org/")]
	[WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
	[System.ComponentModel.ToolboxItem(false)]
	[System.Web.Script.Services.ScriptService]

	public class ProjectServices : System.Web.Services.WebService
	{
		////////////////////////////////////////////////////////////////////////
		///replace the values of these variables with your database credentials
		////////////////////////////////////////////////////////////////////////
		private string dbID = "sprc2023team11";
		private string dbPass = "sprc2023team11";
		private string dbName = "sprc2023team11";
		////////////////////////////////////////////////////////////////////////
		
		////////////////////////////////////////////////////////////////////////
		///call this method anywhere that you need the connection string!
		////////////////////////////////////////////////////////////////////////
		private string getConString() {
			return "SERVER=107.180.1.16; PORT=3306; DATABASE=" + dbName+"; UID=" + dbID + "; PASSWORD=" + dbPass;
		}
		////////////////////////////////////////////////////////////////////////
		

		/////////////////////////////////////////////////////////////////////////
		//don't forget to include this decoration above each method that you want
		//to be exposed as a web service!
		[WebMethod(EnableSession = true)]
		/////////////////////////////////////////////////////////////////////////
		public string TestConnection()
		{
			try
			{
				string testQuery = "select * from users";

				////////////////////////////////////////////////////////////////////////
				///here's an example of using the getConString method!
				////////////////////////////////////////////////////////////////////////
				MySqlConnection con = new MySqlConnection(getConString());
				////////////////////////////////////////////////////////////////////////

				MySqlCommand cmd = new MySqlCommand(testQuery, con);
				MySqlDataAdapter adapter = new MySqlDataAdapter(cmd);
				DataTable table = new DataTable();
				adapter.Fill(table);
				return "Success!";
			}
			catch (Exception e)
			{
				return "Something went wrong, please check your credentials and db name and try again.  Error: "+e.Message;
			}
		}
		
		[WebMethod(EnableSession = true)] //NOTICE: gotta enable session on each individual method
		public bool LogOn(string email, string pass)
		{
			bool success = false;
			
			string sqlSelect = "SELECT userID FROM users WHERE email=@emailValue and pword=@passValue";

			MySqlConnection sqlConnection = new MySqlConnection(getConString());
			MySqlCommand sqlCommand = new MySqlCommand(sqlSelect, sqlConnection);
			
			sqlCommand.Parameters.AddWithValue("@emailValue", HttpUtility.UrlDecode(email));
			sqlCommand.Parameters.AddWithValue("@passValue", HttpUtility.UrlDecode(pass));

			MySqlDataAdapter sqlDa = new MySqlDataAdapter(sqlCommand);
			DataTable sqlDt = new DataTable();
			sqlDa.Fill(sqlDt);
			
			if (sqlDt.Rows.Count > 0)
			{
				Session["userID"] = sqlDt.Rows[0]["userID"];
				success = true;
				// call a function that can connect to database again and store user login time or any details 
				// into the loginstatus table
				// int userid = int.Parse(Session["userID"].ToString());
				// UpdateLoginStatus(userid, "logged in");
			}
			//return the result!
			return success;
		}

		[WebMethod(EnableSession = true)]
		public bool LogOff()
		{
			//if they log off, then we remove the session.  That way, if they access
			//again later they have to log back on in order for their ID to be back
			//in the session!
			Session.Abandon();
			int userid = int.Parse(Session["userID"].ToString());
			UpdateLoginStatus(userid,"logged off");
			return true;
		}
		
		public void UpdateLoginStatus(int id, string log)
		{
			AllowChange();

			string sqlSelect = "insert into login_status (id, time, log) values(@idValue, now(), @logValue);";
			
			MySqlConnection sqlConnection = new MySqlConnection(getConString());
			MySqlCommand sqlCommand = new MySqlCommand(sqlSelect, sqlConnection);
			
			sqlCommand.Parameters.AddWithValue("@idValue", id);
			sqlCommand.Parameters.AddWithValue("@logValue", log);

			sqlConnection.Open();
			//we're using a try/catch so that if the query errors out we can handle it gracefully
			//by closing the connection and moving on
			Console.WriteLine("Executing query...");
			try
			{
				sqlCommand.ExecuteNonQuery();
				Console.WriteLine("Query executed");
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
			}

			sqlConnection.Close(); 
		}

		public void AllowChange()
		{
			string sqlSet = "set foreign_key_checks = 0;";
			
			MySqlConnection sqlConnection = new MySqlConnection(getConString());
			MySqlCommand sqlCommand = new MySqlCommand(sqlSet, sqlConnection);
			
			sqlConnection.Open();
			
			try
			{
				sqlCommand.ExecuteNonQuery();
				Console.WriteLine("Foreign key query executed");
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
			}

			sqlConnection.Close(); 
		}
		
		
		[WebMethod(EnableSession = true)]
		public Account[] StoreAccounts()
		{
			DataTable sqlDt = new DataTable("accounts");

			// string sqlConnectString = System.Configuration.ConfigurationManager.ConnectionStrings["myDB"].ConnectionString;
			string sqlSelect = "select email, pword, roles from users where userID=@idValue;";

			MySqlConnection sqlConnection = new MySqlConnection(getConString());
			MySqlCommand sqlCommand = new MySqlCommand(sqlSelect, sqlConnection);

			sqlCommand.Parameters.AddWithValue("@idValue", Session["userID"]);

			
			//gonna use this to fill a data table
			MySqlDataAdapter sqlDa = new MySqlDataAdapter(sqlCommand);
			//filling the data table
			sqlDa.Fill(sqlDt);
			
			List<Account> accounts = new List<Account>();
			for (int i = 0; i < sqlDt.Rows.Count; i++)
			{
				accounts.Add(new Account
				{
					email = sqlDt.Rows[i]["email"].ToString(),
					password = sqlDt.Rows[i]["pword"].ToString(),
					role = sqlDt.Rows[i]["roles"].ToString(),
				});
			}
			//convert the list of accounts to an array and return!
			return accounts.ToArray();
		}
		

		[WebMethod(EnableSession = true)]
		public void RequestAccount(string pass, string email, string role)
		{
			// string sqlConnectString = System.Configuration.ConfigurationManager.ConnectionStrings["myDB"].ConnectionString;
			//the only thing fancy about this query is SELECT LAST_INSERT_ID() at the end.  All that
			//does is tell mySql server to return the primary key of the last inserted row.
			string sqlSelect = "insert into users (email, pword, roles) values (@emailValue, @passValue, @roleValue); SELECT LAST_INSERT_ID();";
			
			MySqlConnection sqlConnection = new MySqlConnection(getConString());
			MySqlCommand sqlCommand = new MySqlCommand(sqlSelect, sqlConnection);

			sqlCommand.Parameters.AddWithValue("@emailValue", HttpUtility.UrlDecode(email));
			sqlCommand.Parameters.AddWithValue("@passValue", HttpUtility.UrlDecode(pass));
			sqlCommand.Parameters.AddWithValue("@roleValue", HttpUtility.UrlDecode(role));

			sqlConnection.Open();
			//we're using a try/catch so that if the query errors out we can handle it gracefully
			//by closing the connection and moving on
			Console.WriteLine("Executing query...");
			try
			{
				int accountID = Convert.ToInt32(sqlCommand.ExecuteScalar());
				//here, you could use this accountID for additional queries regarding
				//the requested account.  Really this is just an example to show you
				//a query where you get the primary key of the inserted row back from
				//the database!
				
				// we want to make sure subtype (Mentor/Mentee) has the same ID as supertype's userID
				CreateRole(accountID, role);
				Session["userID"] = accountID;
				Session["role"] = role;
			}
			catch (Exception e) {
				Console.WriteLine(e);
			}
			sqlConnection.Close();
		}

		public void CreateRole(int userid, string roleType)
		{
			Console.WriteLine("Executing mentor/mentee create...");
			if (roleType == "mentor")
			{
				string sqlSelect = "insert into Mentors (mentorID) values (@mentorValue); SELECT LAST_INSERT_ID();";
			
				MySqlConnection sqlConnection = new MySqlConnection(getConString());
				MySqlCommand sqlCommand = new MySqlCommand(sqlSelect, sqlConnection);

				sqlCommand.Parameters.AddWithValue("@mentorValue", userid);

				sqlConnection.Open();
				//we're using a try/catch so that if the query errors out we can handle it gracefully
				//by closing the connection and moving on
				try
				{
					sqlCommand.ExecuteNonQuery();
				}
				catch (Exception e) {
					Console.WriteLine(e);
				}
				sqlConnection.Close();
			} else 
			{
				string sqlSelect = "insert into Mentees (menteeID) values (@menteeValue); SELECT LAST_INSERT_ID();";
			
				MySqlConnection sqlConnection = new MySqlConnection(getConString());
				MySqlCommand sqlCommand = new MySqlCommand(sqlSelect, sqlConnection);

				sqlCommand.Parameters.AddWithValue("@menteeValue", userid);

				sqlConnection.Open();
				//we're using a try/catch so that if the query errors out we can handle it gracefully
				//by closing the connection and moving on
				try
				{
					sqlCommand.ExecuteNonQuery();
				}
				catch (Exception e) {
					Console.WriteLine(e);
				}
				sqlConnection.Close();
			}
		}
		
		
		[WebMethod(EnableSession = true)]
		public void CreateProfiles(string fname, string lname, string bday, string phone, string comp, string years, string pos, string bio, string python, string java, string sql)
		{
			// string sqlConnectString = System.Configuration.ConfigurationManager.ConnectionStrings["myDB"].ConnectionString;
			//the only thing fancy about this query is SELECT LAST_INSERT_ID() at the end.  All that
			//does is tell mySql server to return the primary key of the last inserted row.

			Console.WriteLine("Executing profile creation...");

			
			string sqlSelect = " ";
			string picName = Session["userID"].ToString() + ".png";
			Console.WriteLine(Session["role"] + picName + fname + lname + bday + phone + comp + years + pos + bio + python + java + sql);

			if ((string)Session["role"] == "mentor")
			{
				sqlSelect = "Update Mentors Set fname=@fnameValue, lname=@lnameValue, company=@companyValue, phone=@phoneValue," +
				            "experienceYears=@yearsValue, positionRole=@positionValue, birthday=@bdayValue, profileBio=@bioValue," +
				            "profilePic=@picValue, pythonOption=@pythonValue, javaOption=@javaValue, sqlOption=@sqlValue where mentorID=@idValue";
				// sqlSelect = "Update Mentors Set lname=@lnameValue where mentorID=@idValue;";
				
			}
			else
			{
				sqlSelect = "Update Mentees Set fname=@fnameValue, lname=@lnameValue, company=@companyValue, phone=@phoneValue," +
				            "experienceYears=@yearsValue, positionRole=@positionValue, birthday=@bdayValue, profileBio=@bioValue," +
				            "profilePic=@picValue, pythonOption=@pythonValue, javaOption=@javaValue, sqlOption=@sqlValue where menteeID=@idValue";
			}
			
			Console.WriteLine(sqlSelect);
			
			MySqlConnection sqlConnection = new MySqlConnection(getConString());
			MySqlCommand sqlCommand = new MySqlCommand(sqlSelect, sqlConnection);

			sqlCommand.Parameters.AddWithValue("@fnameValue", HttpUtility.UrlDecode(fname));
			sqlCommand.Parameters.AddWithValue("@lnameValue", HttpUtility.UrlDecode(lname));
			sqlCommand.Parameters.AddWithValue("@bdayValue", HttpUtility.UrlDecode(bday));
			sqlCommand.Parameters.AddWithValue("@phoneValue", HttpUtility.UrlDecode(phone));
			sqlCommand.Parameters.AddWithValue("@companyValue", HttpUtility.UrlDecode(comp));
			sqlCommand.Parameters.AddWithValue("@positionValue", HttpUtility.UrlDecode(pos));
			sqlCommand.Parameters.AddWithValue("@yearsValue", HttpUtility.UrlDecode(years));
			sqlCommand.Parameters.AddWithValue("@javaValue", Convert.ToBoolean(HttpUtility.UrlDecode(java)));
			sqlCommand.Parameters.AddWithValue("@pythonValue", Convert.ToBoolean(HttpUtility.UrlDecode(python)));
			sqlCommand.Parameters.AddWithValue("@sqlValue", Convert.ToBoolean(HttpUtility.UrlDecode(sql)));
			sqlCommand.Parameters.AddWithValue("@bioValue", HttpUtility.UrlDecode(bio));
			sqlCommand.Parameters.AddWithValue("@picValue", picName);
			sqlCommand.Parameters.AddWithValue("@idValue", Session["userID"]);

			sqlConnection.Open();
			//we're using a try/catch so that if the query errors out we can handle it gracefully
			//by closing the connection and moving on
			Console.WriteLine("Executing query...");
			try
			{
				sqlCommand.ExecuteNonQuery();
				Console.WriteLine("success");
			}
			catch (Exception e) {
				Console.WriteLine(e);
			}
			sqlConnection.Close();
		}
		
		
		[WebMethod(EnableSession = true)]
		public string LoadCurrentRole()
		{
			DataTable sqlDt = new DataTable("accounts");

			// string sqlConnectString = System.Configuration.ConfigurationManager.ConnectionStrings["myDB"].ConnectionString;
			// select latest login id
			string sqlSelect = "select roles from users where userID=@idValue;";

			MySqlConnection sqlConnection = new MySqlConnection(getConString());
			MySqlCommand sqlCommand = new MySqlCommand(sqlSelect, sqlConnection);
			
			sqlCommand.Parameters.AddWithValue("@idValue", Session["userID"]);

			//gonna use this to fill a data table
			MySqlDataAdapter sqlDa = new MySqlDataAdapter(sqlCommand);
			//filling the data table
			sqlDa.Fill(sqlDt);

			string role = "";
			for (int i = 0; i < sqlDt.Rows.Count; i++)
			{
				role = sqlDt.Rows[i]["roles"].ToString();
				Console.WriteLine(role);
			}
			//return role
			return role;
		}
		
		
		[WebMethod(EnableSession = true)]
		public Profile[] StoreProfiles(string role)
		{
			Console.WriteLine("Executing profile...");
			Console.WriteLine(role);
			
			DataTable sqlDt = new DataTable("accounts");
			
			string sqlSelect = " ";
			
			if (role == "mentor")
			{
				sqlSelect = "select * from Mentors where mentorID=@idValue;";
			}
			else
			{
				sqlSelect = "select * from Mentees where menteeID=@idValue;";
			}

			MySqlConnection sqlConnection = new MySqlConnection(getConString());
			MySqlCommand sqlCommand = new MySqlCommand(sqlSelect, sqlConnection);
			
			sqlCommand.Parameters.AddWithValue("@idValue", Session["userID"]);

			//gonna use this to fill a data table
			MySqlDataAdapter sqlDa = new MySqlDataAdapter(sqlCommand);
			//filling the data table
			sqlDa.Fill(sqlDt);
			
			// change class to name to more general name
			List<Profile> profiles = new List<Profile>();
			for (int i = 0; i < sqlDt.Rows.Count; i++)
			{
				profiles.Add(new Profile
				{
					fname = sqlDt.Rows[i]["fname"].ToString(),
					lname = sqlDt.Rows[i]["lname"].ToString(),
					company = sqlDt.Rows[i]["company"].ToString(),
					phone = sqlDt.Rows[i]["phone"].ToString(),
					years = sqlDt.Rows[i]["experienceYears"].ToString(),
					birthday = sqlDt.Rows[i]["birthday"].ToString(),
					position = sqlDt.Rows[i]["positionRole"].ToString(),
					bio = sqlDt.Rows[i]["profileBio"].ToString(),
					picture = sqlDt.Rows[i]["profilePic"].ToString(),
					python = Convert.ToBoolean(sqlDt.Rows[i]["pythonOption"]),
					java = Convert.ToBoolean(sqlDt.Rows[i]["javaOption"]),
					sql = Convert.ToBoolean(sqlDt.Rows[i]["sqlOption"])
				});
			}
			//convert the list of accounts to an array and return!
			return profiles.ToArray();
		}
		
		[WebMethod(EnableSession = true)]
		public void CreateQuests(string title, string check1, string check2, string check3, string check4, string check5)
		{
			Console.WriteLine(title + Session["userID"]+ check1 + check2 + check3 + check4 + check5);
			
			// string sqlSelect = "insert into Courses (courseName, courseCreatorID, check1, check2, check3, check4, check5) " +
			//                    "values (@titleValue, @idValue, @check1Value, @check2Value, @check3Value, @check4Value, @check5Value)" +
			//                    "; SELECT LAST_INSERT_ID();";

			
			string sqlSelect = "SET FOREIGN_KEY_CHECKS = 0;";
			
			sqlSelect += "insert into Courses (courseName, courseCreatorID, check1, check2, check3, check4, check5) " +
			            "values (@titleValue, @idValue, @check1Value, @check2Value, @check3Value, @check4Value, @check5Value);";

			sqlSelect += "SET FOREIGN_KEY_CHECKS = 1;";

			Console.WriteLine(sqlSelect);
				
			MySqlConnection sqlConnection = new MySqlConnection(getConString());
			MySqlCommand sqlCommand = new MySqlCommand(sqlSelect, sqlConnection);

			sqlCommand.Parameters.AddWithValue("@titleValue", HttpUtility.UrlDecode(title));
			sqlCommand.Parameters.AddWithValue("@idValue", Session["userID"]);
			sqlCommand.Parameters.AddWithValue("@check1Value", HttpUtility.UrlDecode(check1));
			sqlCommand.Parameters.AddWithValue("@check2Value", HttpUtility.UrlDecode(check2));
			sqlCommand.Parameters.AddWithValue("@check3Value", HttpUtility.UrlDecode(check3));
			sqlCommand.Parameters.AddWithValue("@check4Value", HttpUtility.UrlDecode(check4));
			sqlCommand.Parameters.AddWithValue("@check5Value", HttpUtility.UrlDecode(check5));

			sqlConnection.Open();
			//we're using a try/catch so that if the query errors out we can handle it gracefully
			//by closing the connection and moving on
			Console.WriteLine("Executing query...");
			try
			{
				Console.WriteLine("executing...");
				sqlCommand.ExecuteNonQuery();
			}
			catch (Exception e) {
				Console.WriteLine(e);
			}
			sqlConnection.Close();
		}
		
		[WebMethod(EnableSession = true)]
		public Profile[] getQuestMembers(string role)
		{
			Console.WriteLine("Executing profile...");
			Console.WriteLine(role);
			
			DataTable sqlDt = new DataTable("accounts");
			
			string sqlSelect = " ";
			
			if (role == "mentee")
			{
				sqlSelect = "select * from Mentors;";
			}
			else
			{
				Console.WriteLine("Logged in as Mentor, functionality coming soon");
			}
		
			MySqlConnection sqlConnection = new MySqlConnection(getConString());
			MySqlCommand sqlCommand = new MySqlCommand(sqlSelect, sqlConnection);
		
			//gonna use this to fill a data table
			MySqlDataAdapter sqlDa = new MySqlDataAdapter(sqlCommand);
			//filling the data table
			sqlDa.Fill(sqlDt);
			
			// change class to name to more general name
			List<Profile> profiles = new List<Profile>();
			for (int i = 0; i < sqlDt.Rows.Count; i++)
			{
				profiles.Add(new Profile
				{
					id = sqlDt.Rows[i]["mentorID"].ToString(),
					fname = sqlDt.Rows[i]["fname"].ToString(),
					lname = sqlDt.Rows[i]["lname"].ToString(),
					company = sqlDt.Rows[i]["company"].ToString(),
					phone = sqlDt.Rows[i]["phone"].ToString(),
					years = sqlDt.Rows[i]["experienceYears"].ToString(),
					birthday = sqlDt.Rows[i]["birthday"].ToString(),
					position = sqlDt.Rows[i]["positionRole"].ToString(),
					bio = sqlDt.Rows[i]["profileBio"].ToString(),
					picture = sqlDt.Rows[i]["profilePic"].ToString(),
					python = Convert.ToBoolean(sqlDt.Rows[i]["pythonOption"]),
					java = Convert.ToBoolean(sqlDt.Rows[i]["javaOption"]),
					sql = Convert.ToBoolean(sqlDt.Rows[i]["sqlOption"])
				});
			}
			//convert the list of accounts to an array and return!
			return profiles.ToArray();
		}
		
		[WebMethod(EnableSession = true)]
			  
		public List<Quest> getQuests() {
			Console.WriteLine("Executing quests...");
				
			DataTable sqlDt = new DataTable ("quests");
			  
			string sqlSelect = "select * from Courses;";
		
			MySqlConnection sqlConnection = new MySqlConnection(getConString());
			MySqlCommand sqlCommand = new MySqlCommand(sqlSelect, sqlConnection);
			  
			MySqlDataAdapter sqlDa = new MySqlDataAdapter(sqlCommand);
			//filling the data table
			sqlDa.Fill(sqlDt);
			
			List<Quest> quests = new List<Quest>();
			for (int i = 0; i < sqlDt.Rows.Count; i++)
			{
				quests.Add(new Quest
				{
					courseID = Convert.ToInt32(sqlDt.Rows[i]["courseID"]),
					courseName = sqlDt.Rows[i]["courseName"].ToString(),
					courseCreatorID = Convert.ToInt32(sqlDt.Rows[i]["courseCreatorID"]),
					check1 = sqlDt.Rows[i]["check1"].ToString(),
					check2 = sqlDt.Rows[i]["check2"].ToString(),
					check3 = sqlDt.Rows[i]["check3"].ToString(),
					check4 = sqlDt.Rows[i]["check4"].ToString(),
					check5 = sqlDt.Rows[i]["check5"].ToString()
				});
			}
		
			//convert the list of accounts to an array and return!
				return quests;
		}
	}
}
