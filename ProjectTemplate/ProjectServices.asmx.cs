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
		public Account[] checkAccounts()
		{
			DataTable sqlDt = new DataTable("accounts");

			// string sqlConnectString = System.Configuration.ConfigurationManager.ConnectionStrings["myDB"].ConnectionString;
			string sqlSelect = "select email, pword, roles from users;";

			MySqlConnection sqlConnection = new MySqlConnection(getConString());
			MySqlCommand sqlCommand = new MySqlCommand(sqlSelect, sqlConnection);
			
			
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
		public void CreateQuests(string title, string check1, string check2, string check3, string check4, string check5, string description)
		{
			Console.WriteLine("Executing CreatQuest...");
			Console.WriteLine(title + Session["userID"]+ check1 + check2 + check3 + check4 + check5);

			string sqlSelect = "SET FOREIGN_KEY_CHECKS = 0;";
			
			sqlSelect += "insert into Courses (courseName, courseCreatorID, check1, check2, check3, check4, check5, description) " +
			            "values (@titleValue, @idValue, @check1Value, @check2Value, @check3Value, @check4Value, @check5Value, @desValue);";

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
			sqlCommand.Parameters.AddWithValue("@desValue", HttpUtility.UrlDecode(description));


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
		public Profile[] getQuestMembers(string role, int courseID)
		{
			Console.WriteLine("Executing profile...");
			Console.WriteLine(role);
			
			DataTable sqlDt = new DataTable("accounts");
			
			string sqlSelect = " ";
			
			if (role == "mentee")
			{
				sqlSelect = "select Mentors.* from Mentors INNER JOIN Courses on Mentors.mentorID=Courses.courseCreatorID;";
			}
			else
			{
				sqlSelect = "select Mentees.* from Mentees INNER JOIN Progress on Mentees.menteeID=Progress.menteeID where Progress.courseID=@courseID;";
			}
		
			MySqlConnection sqlConnection = new MySqlConnection(getConString());
			MySqlCommand sqlCommand = new MySqlCommand(sqlSelect, sqlConnection);

			if (role == "mentor")
			{
				sqlCommand.Parameters.AddWithValue("@courseID", HttpUtility.UrlDecode(Convert.ToString(courseID)));
			}

			//gonna use this to fill a data table
			MySqlDataAdapter sqlDa = new MySqlDataAdapter(sqlCommand);
			//filling the data table
			sqlDa.Fill(sqlDt);
			
			// change class to name to more general name
			List<Profile> profiles = new List<Profile>();
			for (int i = 0; i < sqlDt.Rows.Count; i++)
			{
				if (role == "mentee"){
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
				else
				{
					profiles.Add(new Profile
					{
						id = sqlDt.Rows[i]["menteeID"].ToString(),
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
				
			}
			//convert the list of accounts to an array and return!
			return profiles.ToArray();
		}
		
		[WebMethod(EnableSession = true)]
			  
		public List<Quest> getQuests(string id) {
			Console.WriteLine("Executing quests...");
				
			DataTable sqlDt = new DataTable ("quests");
			  
			string sqlSelect = "select Courses.* from Courses INNER JOIN Mentors on Mentors.mentorID=Courses.courseCreatorID where mentorID=@mentorID;";
		
			MySqlConnection sqlConnection = new MySqlConnection(getConString());
			MySqlCommand sqlCommand = new MySqlCommand(sqlSelect, sqlConnection);
			
			sqlCommand.Parameters.AddWithValue("@mentorID", HttpUtility.UrlDecode(id));
			
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

		[WebMethod(EnableSession = true)]

		public void Enroll(int courseID)
		{
			string sqlSelect = "insert into Progress(menteeID, courseID, check1, check2, check3, check4, check5, progress) values(@menteeValue, @courseID, false, false, false, false, false, 0);";
			
			MySqlConnection sqlConnection = new MySqlConnection(getConString());
			MySqlCommand sqlCommand = new MySqlCommand(sqlSelect, sqlConnection);

			sqlCommand.Parameters.AddWithValue("@menteeValue", Session["userID"]);
			sqlCommand.Parameters.AddWithValue("@courseID", courseID);
			
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
		
		[WebMethod(EnableSession = true)]
		public Progress[] getProgress()
		{
			Console.WriteLine("Loading progress...");
			Console.WriteLine(Session["userID"]);
			
			DataTable sqlDt = new DataTable("accounts");
			
			string sqlSelect = "select * from Progress where menteeID=@idValue;";

			MySqlConnection sqlConnection = new MySqlConnection(getConString());
			MySqlCommand sqlCommand = new MySqlCommand(sqlSelect, sqlConnection);
			
			sqlCommand.Parameters.AddWithValue("@idValue", Session["userID"]);

			//gonna use this to fill a data table
			MySqlDataAdapter sqlDa = new MySqlDataAdapter(sqlCommand);
			//filling the data table
			sqlDa.Fill(sqlDt);
			
			// change class to name to more general name
			List<Progress> progresses = new List<Progress>();
			for (int i = 0; i < sqlDt.Rows.Count; i++)
			{
				progresses.Add(new Progress
				{
					progressID = Convert.ToInt32(sqlDt.Rows[i]["progressID"]),
					menteeID = Convert.ToInt32(sqlDt.Rows[i]["menteeID"]),
					courseID = Convert.ToInt32(sqlDt.Rows[i]["courseID"]),
					check1 = Convert.ToBoolean(sqlDt.Rows[i]["check1"]),
					check2 = Convert.ToBoolean(sqlDt.Rows[i]["check2"]),
					check3 = Convert.ToBoolean(sqlDt.Rows[i]["check3"]),
					check4 = Convert.ToBoolean(sqlDt.Rows[i]["check4"]),
					check5 = Convert.ToBoolean(sqlDt.Rows[i]["check5"]),
					progress = Convert.ToInt32(sqlDt.Rows[i]["progress"]),
				});
			}
			//convert the list of accounts to an array and return!
			return progresses.ToArray();
		}
		
		[WebMethod(EnableSession = true)]
		public string getAdvancedProgress(string progressID)
		{
			Console.WriteLine(progressID);
			Console.WriteLine("Loading progress...");
			Console.WriteLine(Session["userID"]);
			
			DataTable sqlDt = new DataTable("accounts");
			
			string sqlSelect = "select m.fname, m.lname from Progress p , Courses c , Mentors m " +
			                   "where progressID=@idValue and p.courseID=c.courseID and c.courseCreatorID=m.mentorID;";
			

			MySqlConnection sqlConnection = new MySqlConnection(getConString());
			MySqlCommand sqlCommand = new MySqlCommand(sqlSelect, sqlConnection);
			
			sqlCommand.Parameters.AddWithValue("@idValue", Convert.ToInt32(HttpUtility.UrlDecode(progressID)));

			//gonna use this to fill a data table
			MySqlDataAdapter sqlDa = new MySqlDataAdapter(sqlCommand);
			//filling the data table
			sqlDa.Fill(sqlDt);

			string course_mentor_name = "";
			for (int i = 0; i < sqlDt.Rows.Count; i++)
			{
				course_mentor_name = sqlDt.Rows[i]["fname"].ToString() + " " + sqlDt.Rows[i]["lname"].ToString();
				Console.WriteLine(course_mentor_name);
			}
			//return role
			return course_mentor_name;
		}
		
		[WebMethod(EnableSession = true)]
		public List<Quest> getCourseData(string progressID)
		{
			Console.WriteLine("Loading course data...");
			Console.WriteLine(progressID);

			DataTable sqlDt = new DataTable("quests");
			
			string sqlSelect = "select c.courseName, c.check1, c.check2, c.check3, c.check4, c.check5, c.description from " +
			                   "Progress p ,Courses c where p.progressID=@idValue and p.courseID=c.courseID;";
			
			MySqlConnection sqlConnection = new MySqlConnection(getConString());
			MySqlCommand sqlCommand = new MySqlCommand(sqlSelect, sqlConnection);
			
			sqlCommand.Parameters.AddWithValue("@idValue", Convert.ToInt32(HttpUtility.UrlDecode(progressID)));

			//gonna use this to fill a data table
			MySqlDataAdapter sqlDa = new MySqlDataAdapter(sqlCommand);
			//filling the data table
			sqlDa.Fill(sqlDt);

			List<Quest> quests = new List<Quest>();
			for (int i = 0; i < sqlDt.Rows.Count; i++)
			{
				quests.Add(new Quest
				{
					courseName = sqlDt.Rows[i]["courseName"].ToString(),
					check1 = sqlDt.Rows[i]["check1"].ToString(),
					check2 = sqlDt.Rows[i]["check2"].ToString(),
					check3 = sqlDt.Rows[i]["check3"].ToString(),
					check4 = sqlDt.Rows[i]["check4"].ToString(),
					check5 = sqlDt.Rows[i]["check5"].ToString(),
					description = sqlDt.Rows[i]["description"].ToString()
				});
				Console.WriteLine(quests);
			}
		
			//convert the list of accounts to an array and return!
			return quests;
		}
		
		

		[WebMethod(EnableSession = true)]
		public string LoadMentorName()
		{
			DataTable sqlDt = new DataTable("accounts");

			// string sqlConnectString = System.Configuration.ConfigurationManager.ConnectionStrings["myDB"].ConnectionString;
			// select latest login id
			string sqlSelect = "select fname from Mentors where mentorID=@idValue;";

			MySqlConnection sqlConnection = new MySqlConnection(getConString());
			MySqlCommand sqlCommand = new MySqlCommand(sqlSelect, sqlConnection);
			
			sqlCommand.Parameters.AddWithValue("@idValue", Session["userID"]);

			//gonna use this to fill a data table
			MySqlDataAdapter sqlDa = new MySqlDataAdapter(sqlCommand);
			//filling the data table
			sqlDa.Fill(sqlDt);

			string userResult = "";
			for (int i = 0; i < sqlDt.Rows.Count; i++)
			{
				userResult = sqlDt.Rows[i]["fname"].ToString();
				Console.WriteLine(userResult);
			}
			//return role
			Console.WriteLine("test");

			return userResult;
		}

		[WebMethod(EnableSession = true)]
		public string LoadMenteeName()
		{
			DataTable sqlDt = new DataTable("accounts");

			// string sqlConnectString = System.Configuration.ConfigurationManager.ConnectionStrings["myDB"].ConnectionString;
			// select latest login id
			string sqlSelect = "select fname from Mentees where menteeID=@idValue;";

			MySqlConnection sqlConnection = new MySqlConnection(getConString());
			MySqlCommand sqlCommand = new MySqlCommand(sqlSelect, sqlConnection);
			
			sqlCommand.Parameters.AddWithValue("@idValue", Session["userID"]);

			//gonna use this to fill a data table
			MySqlDataAdapter sqlDa = new MySqlDataAdapter(sqlCommand);
			//filling the data table
			sqlDa.Fill(sqlDt);

			string userResult = "";
			for (int i = 0; i < sqlDt.Rows.Count; i++)
			{
				userResult = sqlDt.Rows[i]["fname"].ToString();
				Console.WriteLine(userResult);
			}
			//return role
			Console.WriteLine("test");

			return userResult;
		}

		[WebMethod(EnableSession = true)]
		public string menteeQuests() {
			Console.WriteLine("Executing quests...");
				
			DataTable sqlDt = new DataTable ("quests");
			  
			//***needs modifying*****
			string sqlSelect = "SELECT courseName FROM Courses;";

			MySqlConnection sqlConnection = new MySqlConnection(getConString());
			MySqlCommand sqlCommand = new MySqlCommand(sqlSelect, sqlConnection);
			
			sqlCommand.Parameters.AddWithValue("@idValue", Session["userID"]);
			
			MySqlDataAdapter sqlDa = new MySqlDataAdapter(sqlCommand);
			//filling the data table
			sqlDa.Fill(sqlDt);
			
			string quests = "";
			for (int i = 0; i < sqlDt.Rows.Count; i++)
			{
				quests = sqlDt.Rows[i]["courseName"].ToString() + "\n";
				Console.WriteLine(quests);
			}

			Console.WriteLine("Executing done...");

		
			//convert the list of accounts to an array and return!
				return quests;
		}
		
		[WebMethod(EnableSession = true)]
		public bool storeProgress(string check1, string check2, string check3, string check4, string check5, string progress, string progressID)
		{
			Console.WriteLine(check1 +check2 + check3 + check4 + check5 + progress + progressID);
			
			bool success = false;
			string sqlSelect = "SET FOREIGN_KEY_CHECKS = 0;";
			
			sqlSelect += "Update Progress Set check1=@c1Value, check2=@c2Value, check3=@c3Value, check4=@c4Value, check5=@c5Value," +
			             "progress=@progressValue Where progressID=@idValue;";

			sqlSelect += "SET FOREIGN_KEY_CHECKS = 1;";
			
			Console.WriteLine(sqlSelect);
			
			MySqlConnection sqlConnection = new MySqlConnection(getConString());
			MySqlCommand sqlCommand = new MySqlCommand(sqlSelect, sqlConnection);
			
			sqlCommand.Parameters.AddWithValue("@c1Value", Convert.ToBoolean(HttpUtility.UrlDecode(check1)));
			sqlCommand.Parameters.AddWithValue("@c2Value", Convert.ToBoolean(HttpUtility.UrlDecode(check2)));
			sqlCommand.Parameters.AddWithValue("@c3Value", Convert.ToBoolean(HttpUtility.UrlDecode(check3)));
			sqlCommand.Parameters.AddWithValue("@c4Value", Convert.ToBoolean(HttpUtility.UrlDecode(check4)));
			sqlCommand.Parameters.AddWithValue("@c5Value", Convert.ToBoolean(HttpUtility.UrlDecode(check5)));
			sqlCommand.Parameters.AddWithValue("@progressValue", Convert.ToDecimal(HttpUtility.UrlDecode(progress)));
			sqlCommand.Parameters.AddWithValue("@idValue", Convert.ToInt32(HttpUtility.UrlDecode(progressID)));
			
			sqlConnection.Open();
			//we're using a try/catch so that if the query errors out we can handle it gracefully
			//by closing the connection and moving on
			Console.WriteLine("Executing query...");
			
			try
			{
				sqlCommand.ExecuteNonQuery();
				success = true;
				Console.WriteLine(success);
			}
			catch (Exception e) {
				Console.WriteLine(e);
				success = false;
			}

			sqlConnection.Close();
			return success;
		}
		
		[WebMethod(EnableSession = true)]
		public Mentee[] getspecificMembers(int courseID)
		{
			Console.WriteLine("Executing profile...");
			
			
			DataTable sqlDt = new DataTable("accounts");

			string sqlSelect = "select c.courseID, m.menteeID, m.fname, m.lname from Mentees m, Progress p, Courses c where p.courseID = c.courseID and p.menteeID=m.menteeID;";

			MySqlConnection sqlConnection = new MySqlConnection(getConString());
			MySqlCommand sqlCommand = new MySqlCommand(sqlSelect, sqlConnection);

			//gonna use this to fill a data table
			MySqlDataAdapter sqlDa = new MySqlDataAdapter(sqlCommand);
			//filling the data table
			sqlDa.Fill(sqlDt);
			
			// change class to name to more general name
			List<Mentee> profiles = new List<Mentee>();
			for (int i = 0; i < sqlDt.Rows.Count; i++)
			{
				
					profiles.Add(new Mentee
					{
						menteeID = sqlDt.Rows[i]["menteeID"].ToString(),
						fname = sqlDt.Rows[i]["fname"].ToString(),
						lname = sqlDt.Rows[i]["lname"].ToString()
					});
				
			}

			return profiles.ToArray();
		}
	}
}
