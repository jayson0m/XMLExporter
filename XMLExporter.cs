/***************************************************
	CMS XML Generator 

	This program compiles all faculty information,
	including their Faculty profile and Image and 
	puts it all into one place to send to SERVER 
	to posting to New web site. 
	
	Most methods are similar in nature.  and Most
	Comments are in Persons Method.

	
	Jayson0m Creation Date: 04-06-16
****************************************************/

using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;
using System.Data.SqlClient;
using System.Configuration;
using System.IO;
using System.Data;
using System.Xml.Schema;
using System.Net.Mail;

namespace XMLExporter
{
    class Program
    {
        #region referenced files consts. schemas, upload folder, log file, bad_xml, faculty images, faculty profile (old Beck)

        public const string XMLSchemaDir = @"\\SERVER\QU_WWW_XML$\Schemas\";

        public const string CMSpath = @"\\SERVER\QU_WWW_XML$\SQL_Upload\";
        public const string CMSlogs = @"\\SERVER\QU_WWW_XML$\Logs\XMLExporter.log";

        public const string badxml = @"\\SERVER\QU_WWW_XML$\Bad_XML\";

        public const string CMSimgpath = CMSpath + @"Faculty_Images\"; // TO // CMS package images

        public const string facultyimgpath = @"\\SERVER\facultyimages$\"; // FROM // Faculty Images 
        public const string facultyprofile = @"\\SERVER\wwwxml$\prebuilt\facultyprofile\current\"; // Faculty Profile		

        #endregion

        static string logfiletext = "";

        static void Main(string[] args)
        { 
            
            
            try
            {
                Console.WriteLine("");

                Console.WriteLine("#####################################################");

                Console.WriteLine("Preparing CMS Persons XML Package...\n\r");
                personxml();
                Console.Write("\n\rDone.\n\r");


                Console.WriteLine("Preparing CMS Course Section XML File...\n\r");
                coursesections();
                Console.Write("\n\rDone.\n\r");

                Console.WriteLine("Preparing CMS Applicants XML File...\n\r");
                Applicants();
                Console.Write("\n\rDone.\n\r");

                Console.WriteLine("Preparing CMS Subjects XML File...\n\r");
                subjects();
                Console.Write("\n\rDone.\n\r");

                Console.WriteLine("Preparing CMS State XML File...\n\r");
                State();
                Console.Write("\n\rDone.\n\r");

                Console.WriteLine("Preparing CMS Course XML File...\n\r");
                Course();
                Console.Write("\n\rDone.\n\r");

                Console.WriteLine("Preparing CMS Terms XML File...\n\r");
                Terms();
                Console.Write("\n\rDone.\n\r");

                Console.WriteLine("Preparing CMS Deans List XML File... \n\r");
                DeansList();
                Console.Write("\n\rDone.\n\r");

                Console.WriteLine("Preparing CMS Organizations XML File... \n\r");
                organization();
                Console.Write("\n\rDone.\n\r");

                Console.WriteLine("Preparing CMS ProgramCourse XML File... \n\r");
                programcourse();
                Console.Write("\n\rDone.\n\r");

            }
            catch (Exception e)
            {
                string line = "{0}: Process failed... - {1}";
                line = String.Format(line, DateTime.Now.ToString(), e.Message.ToString());
                writelog(line, CMSlogs);
            }

            MailMessage mail = new MailMessage("From@Email.Edu", "To@Email.edu");
            mail.To.Add("Additional@email.edu");
            SmtpClient client = new SmtpClient();
            client.Port = 25;
            client.DeliveryMethod = SmtpDeliveryMethod.Network;
            client.UseDefaultCredentials = false;
            client.Host = "mail.server.edu";
            mail.Subject = "CMS XML Log";
            mail.Body = logfiletext;

            try
            {
                client.Send(mail);
            }
            catch (Exception e)
            {
                string line = DateTime.Now.ToString() + ": Exception caught trying to email: " + e.ToString();
                Console.WriteLine(e.ToString());
                writelog(line, CMSlogs);
            }
        }

        #region organization XML for each department (writes Organizations.xml)

        public static void organization()
        {
            string OrganizationFile = CMSpath + "Organizations.xml";
            string dateT = DateTime.Now.ToString(); // current date

            XDocument doc = new XDocument(
                new XDeclaration("1.0", "utf - 16", "true"),
                new XElement("root"));

            doc.Root.Add(new XAttribute("Date_Exported", DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz")));

            XElement MainOrganization = new XElement("Organizations");

            MainOrganization.RemoveAll();

            string connectionString = ConfigurationManager.ConnectionStrings["internet"].ConnectionString;

            string OrganizationSqlStr =
                @"select distinct d.title, ds.campusbuilding, d.phone, d.url, b.latitude, b.longitude, d.lastud, ds.LocalMail, d.eMail
                                        from departmentstaff ds
                                            INNER join DepartmentAEM d on d.departmentid = ds.departmentid
                                            INNER join person p on ds.personid = p.personid
                                            INNER join buildingtemp b on ds.buildingcode = b.building_ID
                                        where d.webdisplay = 1
                                        order by d.title";
            try
            {
                // open SQL connection and execute query string
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    SqlCommand command = new SqlCommand(OrganizationSqlStr, connection);
                    connection.Open();
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        // read each line SQL returns and writes it to XML
                        while (reader.Read())
                        {
                            //DateTime updatedate = Convert.ToDateTime(reader["LastUD"]);
                            XElement RecordNode = new XElement("Record");
                            RecordNode.RemoveAll();
                            RecordNode.Add(new XElement("Department", reader["title"]));
                            RecordNode.Add(new XElement("LocalMail", reader["LocalMail"]));

                            RecordNode.Add(new XElement("CampusBuilding", reader["campusbuilding"]));

                            RecordNode.Add(new XElement("eMail", reader["eMail"]));
                            RecordNode.Add(new XElement("Longitude", reader["longitude"]));
                            RecordNode.Add(new XElement("Latitude", reader["latitude"]));
                            RecordNode.Add(new XElement("Website", reader["url"]));
                            RecordNode.Add(new XElement("Phone", reader["phone"]));
                            RecordNode.Add(new XElement("LastUD",
                                Convert.ToDateTime(reader["LastUD"]).ToString("yyyy-MM-ddTHH:mm:ss.fffzzz")));

                            MainOrganization.Add(RecordNode);
                        }

                        doc.Root.Add(MainOrganization);
                        doc.Save(OrganizationFile);

                        string lines = "Something's wrong";
                        string valid = ValidateXML("Organizations.xml", "Organizations.xsd");
                        if (valid != "1")
                        {
                            lines = dateT + ": Organizations.xml Failed. " + valid;
                        }
                        else
                        {
                            lines = dateT +
                                    ": Organizations.xml: Successfully completed XML build and validation sequence.";
                        }

                        writelog(lines, CMSlogs);
                    }
                }
            }
            catch (Exception e)
            {
                string lines = dateT + ": Organizations.xml Failed. " + e.Message.ToString();
                writelog(lines, CMSlogs);
                Console.Write(e.Message.ToString());
                Console.ReadKey();
                Console.Beep();
            }
        }

        #endregion

        #region Deanslist() Deans list from view (writes DeansList.xml)

        public static void DeansList()
        {
            string coursefile = CMSpath + "DeansList.xml";
            string dateT = DateTime.Now.ToString(); // current date

            XDocument doc = new XDocument(
                new XDeclaration("1.0", "utf - 16", "true"),
                new XElement("root"));

            doc.Root.Add(new XAttribute("Date_Exported", DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz")));

            XElement MainCourseSection = new XElement("DeansList");

            MainCourseSection.RemoveAll();

            string connectionString = ConfigurationManager.ConnectionStrings["DB"].ConnectionString;

            string DeansListSqlStr = "Select * from CMSDeansListView";

            try
            {
                // open SQL connection and execute query string
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    SqlCommand command = new SqlCommand(DeansListSqlStr, connection);
                    connection.Open();
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        // read each line SQL returns and writes it to XML
                        while (reader.Read())
                        {
                            //DateTime updatedate = Convert.ToDateTime(reader["LastUD"]);
                            XElement RecordNode = new XElement("Record");
                            RecordNode.RemoveAll();
                            RecordNode.Add(new XElement("LastName", reader["LastName"]));
                            RecordNode.Add(new XElement("FirstName", reader["FirstName"]));

                            RecordNode.Add(new XElement("MiddleName", reader["MiddleName"]));
                            RecordNode.Add(new XElement("State", reader["State"]));
                            RecordNode.Add(new XElement("StateName", reader["StateName"]));
                            RecordNode.Add(new XElement("Country", reader["Country"]));
                            RecordNode.Add(new XElement("CountryName", reader["CountryName"]));


                            RecordNode.Add(new XElement("LastUD",
                                Convert.ToDateTime(reader["LastUD"]).ToString("yyyy-MM-ddTHH:mm:ss.fffzzz")));

                            MainCourseSection.Add(RecordNode);
                        }

                        doc.Root.Add(MainCourseSection);
                        doc.Save(coursefile);

                        string lines = "";
                        string valid = ValidateXML("DeansList.xml", "DeansList.xsd");
                        if (valid != "1")
                        {
                            lines = dateT + ": DeansList.xml Failed. " + valid;
                        }
                        else
                        {
                            lines = dateT +
                                    ": DeansList.xml: Successfully completed XML build and validation sequence.";
                        }

                        writelog(lines, CMSlogs);
                    }
                }
            }
            catch (Exception e)
            {
                string lines = dateT + ": DeansList.xml Failed. " + e.Message.ToString();
                writelog(lines, CMSlogs);
                Console.Write(e.Message.ToString());
                Console.Beep();
            }
        }

        #endregion

        #region List of Courses pulled from view (Writes courses.xml)

        public static void Course()
        {
            string coursefile = CMSpath + "Courses.xml";
            string dateT = DateTime.Now.ToString(); // current date

            XDocument doc = new XDocument(
                new XDeclaration("1.0", "utf - 16", "true"),
                new XElement("root"));

            doc.Root.Add(new XAttribute("Date_Exported", DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz")));

            XElement MainCourseSection = new XElement("Courses");

            MainCourseSection.RemoveAll();

            string connectionString = ConfigurationManager.ConnectionStrings["DB"].ConnectionString;

            string courseSqlStr = "Select * from CMSCourseView";

            try
            {
                // open SQL connection and execute query string
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    SqlCommand command = new SqlCommand(courseSqlStr, connection);
                    connection.Open();
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        // read each line SQL returns and writes it to XML
                        while (reader.Read())
                        {
                            //DateTime updatedate = Convert.ToDateTime(reader["LastUD"]);
                            XElement RecordNode = new XElement("Record");
                            RecordNode.RemoveAll();
                            RecordNode.Add(new XElement("CourseID", reader["CourseID"]));
                            RecordNode.Add(new XElement("Name", reader["Name"]));

                            RecordNode.Add(new XElement("Title", reader["Title"]));
                            RecordNode.Add(new XElement("Description", reader["Description"]));
                            RecordNode.Add(new XElement("Subject", reader["Subject"]));
                            RecordNode.Add(new XElement("Number", reader["Number"]));


                            RecordNode.Add(new XElement("LastUD",
                                Convert.ToDateTime(reader["LastUD"]).ToString("yyyy-MM-ddTHH:mm:ss.fffzzz")));

                            MainCourseSection.Add(RecordNode);
                        }

                        doc.Root.Add(MainCourseSection);
                        doc.Save(coursefile);

                        string lines = "Something's wrong";
                        string valid = ValidateXML("Courses.xml", "Courses.xsd");
                        if (valid != "1")
                        {
                            lines = dateT + ": Courses.xml Failed. " + valid;
                        }
                        else
                        {
                            lines = dateT + ": Courses.xml - Successfully completed XML build and validation sequence.";
                        }

                        writelog(lines, CMSlogs);
                    }
                }
            }
            catch (Exception e)
            {
                string lines = dateT + ": Courses.xml Failed. " + e.Message.ToString();
                writelog(lines, CMSlogs);
                Console.Write(e.Message.ToString());
                Console.Beep();
            }
        }

        #endregion

        #region Terms view (writes Terms.xml)

        public static void Terms()
        {
            string statefile = CMSpath + "Terms.xml";
            string dateT = DateTime.Now.ToString(); // current date

            XDocument doc = new XDocument(
                new XDeclaration("1.0", "utf - 16", "true"),
                new XElement("root"));

            doc.Root.Add(new XAttribute("Date_Exported", DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz")));

            XElement MainCourseSection = new XElement("Terms");

            MainCourseSection.RemoveAll();

            string connectionString = ConfigurationManager.ConnectionStrings["DB"].ConnectionString;

            string TermsSqlStr = @"Select * from CMSTermsView";

            try
            {
                // open SQL connection and execute query string
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    SqlCommand command = new SqlCommand(TermsSqlStr, connection);
                    connection.Open();
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        // read each line SQL returns and writes it to XML
                        while (reader.Read())
                        {
                            
                            XElement RecordNode = new XElement("Record");
                            RecordNode.RemoveAll();
                            RecordNode.Add(new XElement("Term_ID", reader["Term_ID"]));
                            RecordNode.Add(new XElement("Title", reader["Title"]));
                            RecordNode.Add(new XElement("StartDate",
                                Convert.ToDateTime(reader["StartDate"]).ToString("yyyy-MM-ddTHH:mm:ss.fffzzz")));
                            RecordNode.Add(new XElement("EndDate",
                                Convert.ToDateTime(reader["EndDate"]).ToString("yyyy-MM-ddTHH:mm:ss.fffzzz")));

                            RecordNode.Add(new XElement("ReportingTerm", reader["ReportingTerm"]));
                            RecordNode.Add(new XElement("ReportingYear", reader["ReportingYear"]));
                            RecordNode.Add(new XElement("Session", reader["Session"]));

                            RecordNode.Add(new XElement("WebDate",
                                Convert.ToDateTime(reader["WebDate"]).ToString("yyyy-MM-ddTHH:mm:ss.fffzzz")));
                            RecordNode.Add(new XElement("LastUD",
                                Convert.ToDateTime(reader["LastUD"]).ToString("yyyy-MM-ddTHH:mm:ss.fffzzz")));

                            MainCourseSection.Add(RecordNode);
                        }

                        doc.Root.Add(MainCourseSection);
                        doc.Save(statefile);

                        string lines = "Something's wrong";
                        string valid = ValidateXML("Terms.xml", "Terms.xsd");
                        if (valid != "1")
                        {
                            lines = dateT + ": Terms.xml Failed. " + valid;
                        }
                        else
                        {
                            lines = dateT + ": Terms.xml - Successfully completed XML build and validation sequence.";
                        }

                        writelog(lines, CMSlogs);
                    }
                }
            }
            catch (Exception e)
            {
                string lines = dateT + ": Terms.xml Failed. " + e.Message.ToString();
                writelog(lines, CMSlogs);
                Console.Write(e.Message.ToString());
                Console.Beep();
            }
        }

        #endregion

        #region state() Pulls all states (Writes State.xml)

        public static void State()
        {
            string statefile = CMSpath + "State.xml";
            string dateT = DateTime.Now.ToString(); // current date

            XDocument doc = new XDocument(
                new XDeclaration("1.0", "utf - 16", "true"),
                new XElement("root"));

            doc.Root.Add(new XAttribute("Date_Exported", DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz")));

            XElement MainCourseSection = new XElement("State");

            MainCourseSection.RemoveAll();

            string connectionString = ConfigurationManager.ConnectionStrings["DB"].ConnectionString;

            string StateSqlStr = @"Select * from CMSStateView";

            try
            {
                // open SQL connection and execute query string
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    SqlCommand command = new SqlCommand(StateSqlStr, connection);
                    connection.Open();
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        int x = 1;
                        // read each line SQL returns and writes it to XML
                        while (reader.Read())
                        {
                            XElement RecordNode = new XElement("Record");
                            RecordNode.RemoveAll();
                            
                            RecordNode.Add(new XElement("State_ID", reader["State_ID"]));
                            RecordNode.Add(new XElement("Title", reader["Title"]));
                            RecordNode.Add(new XElement("LastUD",
                                Convert.ToDateTime(reader["LastUD"]).ToString("yyyy-MM-ddTHH:mm:ss.fffzzz")));
                            
                            MainCourseSection.Add(RecordNode);
                            x++;
                        }


                        doc.Root.Add(MainCourseSection);
                        doc.Save(statefile);

                        string lines = "Something's wrong";
                        string valid = ValidateXML("State.xml", "State.xsd");
                        if (valid != "1")
                        {
                            lines = dateT + ": State.xml Failed. " + valid;
                        }
                        else
                        {
                            lines = dateT + ": State.xml - Successfully completed XML build and validation sequence.";
                        }

                        writelog(lines, CMSlogs);
                    }
                }
            }
            catch (Exception e)
            {
                string lines = dateT + ": State.xml Failed. " + e.Message.ToString();
                writelog(lines, CMSlogs);
                Console.Write(e.Message.ToString());
                Console.Beep();
            }
        }

        #endregion

        #region subjects () pulls all subjects (writes Subjects.xml)

        public static void subjects()
        {
            string Subjectfile = CMSpath + "Subjects.xml";
            string dateT = DateTime.Now.ToString(); // current date

            XDocument doc = new XDocument(
                new XDeclaration("1.0", "utf - 16", "true"),
                new XElement("root"));

            doc.Root.Add(new XAttribute("Date_Exported", DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz")));

            XElement MainCourseSection = new XElement("Subjects");

            MainCourseSection.RemoveAll();

            string connectionString = ConfigurationManager.ConnectionStrings["DB"].ConnectionString;

            string SubjectSqlStr = @"Select * from CMSSubjectView";

            try
            {
                // open SQL connection and execute query string
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    SqlCommand command = new SqlCommand(SubjectSqlStr, connection);
                    connection.Open();
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        // read each line SQL returns and writes it to XML
                        while (reader.Read())
                        {
                            //DateTime updatedate = Convert.ToDateTime(reader["LastUD"]);
                            XElement RecordNode = new XElement("Record");
                            RecordNode.RemoveAll();
                            RecordNode.Add(new XElement("Subject_ID", reader["Subject_ID"]));
                            RecordNode.Add(new XElement("Title", reader["Title"]));
                            RecordNode.Add(new XElement("LastUD",
                                Convert.ToDateTime(reader["LastUD"]).ToString("yyyy-MM-ddTHH:mm:ss.fffzzz")));

                            MainCourseSection.Add(RecordNode);
                        }

                        doc.Root.Add(MainCourseSection);
                        doc.Save(Subjectfile);

                        string lines = "Something's wrong";
                        string valid = ValidateXML("Subjects.xml", "Subjects.xsd");
                        if (valid != "1")
                        {
                            lines = dateT + ": Subjects.xml Failed. " + valid;
                        }
                        else
                        {
                            lines = dateT +
                                    ": Subjects.xml - Successfully completed XML build and validation sequence.";
                        }

                        writelog(lines, CMSlogs);
                    }
                }
            }
            catch (Exception e)
            {
                string lines = dateT + ": Subject.xml Failed. " + e.Message.ToString();
                writelog(lines, CMSlogs);
                Console.Write(e.Message.ToString());
                Console.Beep();
            }
        }

        #endregion

        #region Applicants() gets applicant information (Writes Applicants.xml)

        public static void Applicants()
        {
            string Applicantsfile = CMSpath + "Applicants.xml";
            string dateT = DateTime.Now.ToString(); // current date

            XDocument doc = new XDocument(
                new XDeclaration("1.0", "utf - 16", "true"),
                new XElement("root"));

            doc.Root.Add(new XAttribute("Date_Exported", DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz")));

            XElement MainCourseSection = new XElement("Applicants");

            MainCourseSection.RemoveAll();

            string connectionString = ConfigurationManager.ConnectionStrings["DB"].ConnectionString;
            string ApplicantsSqlStr = @"Select * from CMSApplicantsView";

            try
            {
                // open SQL connection and execute query string
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    SqlCommand command = new SqlCommand(ApplicantsSqlStr, connection);
                    connection.Open();
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        // read each line SQL returns and writes it to XML
                        while (reader.Read())
                        {
                            //DateTime updatedate = Convert.ToDateTime(reader["LastUD"]);
                            XElement RecordNode = new XElement("Record");
                            RecordNode.RemoveAll();
                            RecordNode.Add(new XElement("Person_ID", reader["Person_ID"]));
                            RecordNode.Add(new XElement("lastName", reader["lastName"]));
                            RecordNode.Add(new XElement("FirstName", reader["FirstName"]));
                            RecordNode.Add(new XElement("MiddleName", reader["MiddleName"]));
                            RecordNode.Add(new XElement("Zip", reader["Zip"]));
                            RecordNode.Add(new XElement("EMailAddr1", reader["EMailAddr1"]));
                            RecordNode.Add(new XElement("CurrentStatus", reader["CurrentStatus"]));
                            RecordNode.Add(new XElement("LastUD",
                                Convert.ToDateTime(reader["LastUD"]).ToString("yyyy-MM-ddTHH:mm:ss.fffzzz")));

                            string xDate = "{0}/{1}";
                            string status = "X";
                            if (reader["DateRestriction"].ToString() != "" && reader["ReportingYear"].ToString() != "")
                            {
                                xDate = String.Format(xDate, reader["DateRestriction"].ToString(),
                                    reader["ReportingYear"].ToString());

                                DateTime yDate = Convert.ToDateTime(xDate);
                                DateTime myDate = DateTime.Now; //.ToString("MM/dd/yyyy");

                                if (yDate > myDate)
                                {
                                    status = "A";
                                }
                                else
                                {
                                    status = "B";
                                }
                            }
                            else
                            {
                                status = "B";
                            }

                            RecordNode.Add(new XElement("CurrentMessageFlag", status));

                            MainCourseSection.Add(RecordNode);
                        }

                        doc.Root.Add(MainCourseSection);
                        doc.Save(Applicantsfile);
                        string lines = "Something's wrong";

                        string valid = ValidateXML("Applicants.xml", "Applicants.xsd");
                        if (valid != "1")
                        {
                            lines = dateT + ": Applicants.xml Failed. " + valid;
                        }
                        else
                        {
                            lines = dateT +
                                    ": Applicants.xml - Successfully completed XML build and validation sequence.";
                        }

                        writelog(lines, CMSlogs);

                        Console.WriteLine("CMS Applicants.xml package complete.");
                    }
                }
            }
            catch (Exception e)
            {
                string lines = dateT + ": Applicants.xml Failed. " + e.Message.ToString();
                writelog(lines, CMSlogs);
                Console.Write(e.Message.ToString());
                Console.Beep();
            }
        }

        #endregion

        #region Coursesections() gets course section information (Makes CourseSection.xml)

        public static void coursesections()
        {
            string Coursesectionfile = CMSpath + "CourseSection.xml";
            string dateT = DateTime.Now.ToString(); // current date

            XDocument doc = new XDocument(
                new XDeclaration("1.0", "utf - 16", "true"),
                new XElement("root"));

            doc.Root.Add(new XAttribute("Date_Exported", DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz")));

            XElement MainCourseSection = new XElement("CourseSection");

            MainCourseSection.RemoveAll();

            string connectionString = ConfigurationManager.ConnectionStrings["DB"].ConnectionString;
            string CourseSectionMainSqlStr = @"Select * from CMSCourseSectionView";
            string CourseSectionMeetingSqlStr = @"select * from CMSCourseSectionMeetingView";
            string CourseSectionFacultySqlStr = @"select * from CMSCourseSectionFacultyView";

            DataTable meetingdata = storeData(CourseSectionMeetingSqlStr);
            DataTable facultydata = storeData(CourseSectionFacultySqlStr);

            try
            {
                // open SQL connection and execute query string
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    SqlCommand command = new SqlCommand(CourseSectionMainSqlStr, connection);
                    connection.Open();
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        // read each line SQL returns and writes it to XML
                        while (reader.Read())
                        {
                            //DateTime updatedate = Convert.ToDateTime(reader["LastUD"]);
                            XElement RecordNode = new XElement("Record");
                            RecordNode.RemoveAll();
                            RecordNode.Add(new XElement("CourseSectionID", reader["CourseSectionID"]));
                            RecordNode.Add(new XElement("CourseID", reader["CourseID"]));
                            RecordNode.Add(new XElement("PersonID", reader["PersonID"]));
                            RecordNode.Add(new XElement("Term", reader["Term"]));
                            RecordNode.Add(new XElement("Status", reader["Status"]));
                            RecordNode.Add(new XElement("Name", reader["Name"]));
                            RecordNode.Add(new XElement("Subject", reader["Subject"]));
                            RecordNode.Add(new XElement("CourseNo", reader["CourseNo"]));
                            RecordNode.Add(new XElement("Number", reader["Number"]));
                            RecordNode.Add(new XElement("InstrMethods", reader["InstrMethods"]));
                            RecordNode.Add(new XElement("LastUD",
                                Convert.ToDateTime(reader["LastUD"]).ToString("yyyy-MM-ddTHH:mm:ss.fffzzz")));


                            DataRow[] meetingrows;
                            meetingrows = meetingdata.Select("CourseSectionID = '" + reader["CourseSectionID"] + "'");

                            if (meetingrows.Length != 0)
                            {
                                int m = 0;
                                foreach (var x in meetingrows)
                                {
                                    XElement MeetingNode = new XElement("Meeting");
                                    MeetingNode.Add(new XElement("CourseSectionID",
                                        meetingrows[m]["CourseSectionID"].ToString()));
                                    MeetingNode.Add(new XElement("Building", meetingrows[m]["Building"].ToString()));
                                    MeetingNode.Add(new XElement("Room", meetingrows[m]["Room"].ToString()));
                                    MeetingNode.Add(new XElement("Frequency", meetingrows[m]["Frequency"].ToString()));
                                    MeetingNode.Add(new XElement("StartDateTime",
                                        Convert.ToDateTime(meetingrows[m]["StartDateTime"])
                                            .ToString("yyyy-MM-ddTHH:mm:ss.fffzzz")));
                                    MeetingNode.Add(new XElement("EndDateTime",
                                        Convert.ToDateTime(meetingrows[m]["EndDateTime"])
                                            .ToString("yyyy-MM-ddTHH:mm:ss.fffzzz")));
                                    MeetingNode.Add(new XElement("Days", meetingrows[m]["Days"].ToString()));
                                    MeetingNode.Add(new XElement("LastUD",
                                        Convert.ToDateTime(meetingrows[m]["LastUD"])
                                            .ToString("yyyy-MM-ddTHH:mm:ss.fffzzz")));

                                    m++;

                                    DataRow[] facultyrows;
                                    facultyrows =
                                        facultydata.Select("CourseSectionID = '" + reader["CourseSectionID"] + "'");

                                    if (facultyrows.Length != 0)
                                    {
                                        int i = 0;
                                        foreach (var sx in facultyrows)
                                        {
                                            XElement FacultyNode = new XElement("Faculty");
                                            FacultyNode.Add(new XElement("CourseSectionID",
                                                facultyrows[i]["CourseSectionID"].ToString()));
                                            FacultyNode.Add(new XElement("PersonID",
                                                facultyrows[i]["PersonID"].ToString()));
                                            FacultyNode.Add(new XElement("FacultyLoad",
                                                facultyrows[i]["FacultyLoad"].ToString()));
                                            FacultyNode.Add(new XElement("LastUD",
                                                Convert.ToDateTime(facultyrows[i]["LastUD"])
                                                    .ToString("yyyy-MM-ddTHH:mm:ss.fffzzz")));
                                            MeetingNode.Add(FacultyNode);
                                            i++;
                                        }
                                    }

                                    RecordNode.Add(MeetingNode);
                                }
                            }

                            MainCourseSection.Add(RecordNode);
                        }

                        doc.Root.Add(MainCourseSection);
                        doc.Save(Coursesectionfile);
                        string lines = "Something's wrong";

                        string valid = ValidateXML("CourseSection.xml", "CourseSection.xsd");
                        if (valid != "1")
                        {
                            lines = dateT + ": CourseSection.xml Failed. " + valid;
                        }
                        else
                        {
                            lines = dateT +
                                    ": CourseSection.xml - Successfully completed XML build and validation sequence.";
                        }

                        writelog(lines, CMSlogs);

                        Console.WriteLine("CMS CourseSection.xml package complete.");
                    }
                }
            }
            catch (Exception e)
            {
                string lines = dateT + ": CourseSection.xml Failed. " + e.Message.ToString();
                writelog(lines, CMSlogs);
                Console.Write(e.Message.ToString());
                Console.Beep();
            }
        }

        #endregion

        #region Personxml() gets all person data, connects courses to faculty (makes person.xml)

        public static void personxml()
        {
            #region Sql Code... Converted into View to make easier...  

            /*@"
            OLD Lastname field
             * 	case 
				when p.Suffix <> ''  then p.LastName + ', ' + p.Suffix
				else p.lastname
                end as 'lastname',
             
                SELECT p.person_ID,p.PersonID, 
                DirectoryLastName as lastname,
                   p.FirstName, p.MiddleName, fe.eMail, p.NTLoginID, b.latitude, b.longitude,
                        ds.Title, ISNULL(fpo.Title,'') AS 'TitleOther', CAST(f.Degrees as VARCHAR(300)) as 'Degrees' , ds.CampusBuilding, ds.CampusOffice, CampusPhone, ds.LocalMail, 
                        CASE
                        WHEN fdv.DeptTitle IS NOT NULL THEN fdv.DeptTitle
                          ELSE d.title 
                        END  as departmentname,
                        p.LastUD, fc.personid as 'facultyperson',  s.Title as 'schoolname', s.School_ID as 'School-ID', 
                        s.SchoolID, ds.TitleShort as 'proftitle', ds.PositionCode
                FROM Person p join DepartmentStaff ds ON p.PersonID = ds.PersonID AND p.WebDisplay = 1 AND SUBSTRING(ds.PositionCode,1,4) NOT IN ('PRMS','PRNS','RPMS')
                LEFT JOIN FacultyPositionOther fpo ON fpo.PersonID = p.PersonID
                LEFT JOIN Faculty f ON f.PersonID = p.PersonID
                LEFT JOIN FacultyCurrent fc on fc.personid = p.PersonID
                LEFT JOIN buildingtemp b on ds.buildingcode = b.building_ID
                LEFT JOIN Department d on ds.departmentID = d.DepartmentID
                LEFT JOIN School s on d.Division = s.School_ID
                left join facultyemail fe on p.Person_ID = fe.Person_ID
                        or d.school = s.School_ID
                LEFT JOIN FacultyPartTimeDepartmentView fdv ON p.PersonID = fdv.PersonID";	*/

/*                    SELECT p.person_ID,p.PersonID, p.LastName, p.FirstName, p.MiddleName, p.eMail, p.NTLoginID, b.latitude, b.longitude,
                            ds.Title, ISNULL(fpo.Title,'') AS 'TitleOther', CAST(f.Degrees as VARCHAR(300)) as 'Degrees', ds.CampusBuilding, ds.CampusOffice, CampusPhone, ds.LocalMail, 
                            d.title as departmentname, p.LastUD, f.personid as 'facultyperson',  s.Title as 'schoolname', s.School_ID as 'School-ID', 
                            s.SchoolID, ds.TitleShort as 'proftitle' , ds.PositionCode
                    FROM Person p left join DepartmentStaff ds ON p.PersonID = ds.PersonID AND p.WebDisplay = 1 AND SUBSTRING(ds.PositionCode,1,4) NOT IN ('PRMS','PRNS','RPMS')
                    LEFT JOIN FacultyPositionOther fpo ON fpo.PersonID = p.PersonID
                    LEFT JOIN Faculty f ON f.PersonID = p.PersonID
                    LEFT JOIN buildingtemp b on ds.buildingcode = b.building_ID
                    LEFT JOIN Department d on ds.departmentID = d.DepartmentID
                    LEFT JOIN School s on d.Division = s.School_ID
                        or d.school = s.School_ID
					where person_id = '0473226'"; */

            #endregion


            string dateT = DateTime.Now.ToString();
            
            SpecialProfessors lawprofs = new SpecialProfessors();
            //lawprofs.InitiateList();
            
            // create the main person.xml
            XDocument doc = new XDocument(
                new XDeclaration("1.0", "utf - 16", "true"),
                new XElement("root"));
            doc.Root.Add(new XAttribute("Date_Exported", DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz")));


            // Open Connection to DB--password is located in App.Config
            var connectionString = ConfigurationManager.ConnectionStrings["DB"].ConnectionString;

               
            string queryString = "select * from CMSPersonView";

            string personCourses = "select * from CMSCoursesListView"; 
            
            
            DataTable pcourse = storeData(personCourses);

            try
            {
                // open SQL connection and execute query string
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    SqlCommand command = new SqlCommand(queryString, connection);
                    connection.Open();
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        // checks if image folder exists
                        bool exists = System.IO.Directory.Exists(CMSimgpath);
                        if (!exists) // if not create it
                            System.IO.Directory.CreateDirectory(CMSimgpath);

                        string facultydata; // storage for faculty profile data

                        // Clear out all images first.
                        DirectoryInfo dirinfo = new DirectoryInfo(CMSimgpath);
                        
                        foreach (FileInfo file in dirinfo.GetFiles())
                        {
                            file.Delete();
                        }
                        
                        // read each line SQL returns and writes it to XML
                        while (reader.Read())
                        {
                            // opens faculty profile document
                            XmlDocument facdoc = new XmlDocument();

                            string curFile =
                                facultyprofile + reader["NTLoginID"].ToString() +
                                ".xml"; // creates name from NTLoginID 
                            
                            // If profile exists store it... if not blank out that field. 
                            if (File.Exists(curFile))
                            {
                                facdoc.Load(curFile);
                                XmlNode node = facdoc.SelectSingleNode("interests");
                                facultydata = "<data>" + node.InnerXml + "</data>";
                                //facultydata = node.InnerXml;
                            }
                            else
                            {
                                facdoc = null;
                                facultydata = "";
                            }
                            
                            // Added to add manual Law faculty Links... 
                            string lawurl = GetLawURLs(reader["NTLoginID"].ToString());
                            string AString = "View " + reader["FirstName"].ToString() + " " +
                                             reader["LastName"].ToString() + "'s Faculty Profile";  
                                                                         
                            if (lawurl != "0" )
                            {
                                facdoc = null;
                                facultydata = $"<data><bio>&lt;a href='{lawurl}'&gt;{AString}&lt;/a&gt;</bio><sections><section><title/><desc/><files><file><fname/><fdesc/><ftitle/></file></files></section></sections></data>";
                            }
                            
                            facultydata = "";
                            
                            string imgname =
                                reader["Person_ID"].ToString() + ".jpg"; // Find faculty image based on their QU ID # 
                            imgname = imgname.PadLeft(13,
                                '0'); // Images are 13 characters including .jpg. code must pad left
                            string newimgname =
                                reader["PersonID"].ToString() + ".jpg"; // New image name based on personID


                            // If file exists copy it over, overwrite and rename it. if not, move on. 
                            if (File.Exists(facultyimgpath + imgname))
                            {
                                File.Copy(facultyimgpath + imgname, CMSimgpath + newimgname, true);
                            }


                            DateTime updatedate = Convert.ToDateTime(reader["LastUD"]);

                            // Creates the XML tree structure for each record. 
                            DataRow[] courses;

                            courses = pcourse.Select("PersonID = '" + reader["PersonID"] + "'");
                            XElement RecordNode = new XElement("Record");
                            RecordNode.Add(new XElement("PersonID", reader["PersonID"].ToString()));
                            RecordNode.Add(new XElement("LastName", reader["LastName"].ToString()));
                            RecordNode.Add(new XElement("FirstName", reader["FirstName"].ToString()));

                            if (reader["MiddleName"].ToString() != reader["FirstName"].ToString())
                                RecordNode.Add(new XElement("MiddleName", reader["MiddleName"].ToString()));
                            else
                                RecordNode.Add(new XElement("MiddleName", ""));


                            RecordNode.Add(new XElement("eMail", reader["eMail"].ToString()));
                            RecordNode.Add(new XElement("DepartmentName", reader["departmentname"].ToString()));

                            // added to make Discipline 
                            
                            string Discipline = GetDicipline(reader["School-ID"].ToString(),
                                reader["departmentname"].ToString(), reader["Title"].ToString());
                            RecordNode.Add(new XElement("Discipline", Discipline));
                            
                            
                            RecordNode.Add(new XElement("Title", reader["Title"].ToString()));
                            RecordNode.Add(new XElement("TitleOther", reader["TitleOther"].ToString()));
                            RecordNode.Add(new XElement("Degrees", reader["Degrees"].ToString()));
                            RecordNode.Add(new XElement("CampusBuilding", reader["CampusBuilding"].ToString()));
                            RecordNode.Add(new XElement("CampusOffice", reader["CampusOffice"].ToString()));
                            RecordNode.Add(new XElement("CampusPhone", reader["CampusPhone"].ToString()));
                            RecordNode.Add(new XElement("LocalMail", reader["LocalMail"].ToString()));
                            RecordNode.Add(new XElement("Longitude", reader["longitude"].ToString()));
                            RecordNode.Add(new XElement("Latitude", reader["latitude"].ToString()));
                            RecordNode.Add(new XElement("LastUD", updatedate.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz")));

                            if (reader["facultyperson"].ToString() != "")
                            {
                                // Should be faculty... 
                                RecordNode.Add(new XElement("Faculty_Member", "Yes"));
                                RecordNode.Add(new XElement("School", reader["schoolname"].ToString()));
                                RecordNode.Add(new XElement("SchoolID", reader["School-ID"].ToString()));
                                RecordNode.Add(new XElement("ProfTitle", reader["proftitle"].ToString()));
                            }
                            else
                            {
                                RecordNode.Add(new XElement("Faculty_Member", "No"));
                                RecordNode.Add(new XElement("School", ""));
                                RecordNode.Add(new XElement("SchoolID", ""));
                                RecordNode.Add(new XElement("ProfTitle", ""));
                            }

                            RecordNode.Add(new XElement("FacultyProfile", facultydata));

                            int i = 0;
                            XElement CoursesNode = new XElement("Courses");
                            foreach (var x in courses)
                            {
                                XElement CourseNode = new XElement("Course");
                                CourseNode.RemoveAll();
                                CourseNode.Add(new XElement("CourseID", courses[i]["CourseID"]));
                                CourseNode.Add(new XElement("Course_ID", courses[i]["Course_ID"]));
                                CourseNode.Add(new XElement("Title_DataTel", courses[i]["Title_DataTel"]));
                                CourseNode.Add(new XElement("Name", courses[i]["Name"]));
                                CourseNode.Add(new XElement("Title", courses[i]["Title"]));
                                CourseNode.Add(new XElement("Term_id", courses[i]["Term_id"]));
                                i++;
                                CoursesNode.Add(CourseNode);
                            }

                            RecordNode.Add(CoursesNode);
                            doc.Root.Add(RecordNode);
                        }
                    }
                }

                if (File.Exists(CMSpath + "temp.xml"))
                    File.Delete(CMSpath + "temp.xml");

                doc.Save(CMSpath + "temp.xml"); // save temp file to CMS path. 
                // write to log file to CMS Directory XMLExporter.log
                string lines = "Something's wrong";
                string valid = ValidateXML("temp.xml", "Person.xsd");
                if (valid != "1")
                {
                    lines = dateT + ": Person.xml Failed. " + valid;
                }
                else
                {
                    lines = dateT + ": Person.xml - Successfully completed XML build and validation sequence.";
                }

                // clean the file. removes &gt; &lt; replaces them with > < 
                cleanfile(CMSpath + "temp.xml", CMSpath + "Person.xml");


                //clean up if temp is there
                if (File.Exists(CMSpath + "temp.xml"))
                    File.Delete(CMSpath + "temp.xml");


                writelog(lines, CMSlogs);
            }
            catch (Exception e)
            {
                // if we throw an error--pop it up on the screen and stop. Also log to CMS Directory XMLExporter.log

                string lines = dateT + ": Person.xml Failed. " + e.Message.ToString();
                writelog(lines, CMSlogs);
                Console.Write(e.Message);
                Console.Beep();
            }
        }

        #endregion

        #region programcourse() Gets all Programs and courses related to it (makes programcourse.xml)

        public static void programcourse()
        {
            string ProgramCoursefile = CMSpath + "ProgramCourse.xml";
            string dateT = DateTime.Now.ToString(); // current date

            XDocument doc = new XDocument(
                new XDeclaration("1.0", "utf - 16", "true"),
                new XElement("root"));

            doc.Root.Add(new XAttribute("Date_Exported", DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz")));

            XElement MainCourseSection = new XElement("ProgramCourse");

            MainCourseSection.RemoveAll();

            string connectionString = ConfigurationManager.ConnectionStrings["DB"].ConnectionString;
           
           
            string ProgramMainSqlStr = @"select distinct p.programid, p.Title as ProgramName
                                            from Program p join MajorProgram mp on p.ProgramID = mp.ProgramID and p.WebDisplay = 1
                                            join MajorSubject ms on ms.MajorID = mp.MajorID
                                            join Subject s on s.SubjectID = ms.SubjectID
                                            join Course c on c.Subject = s.Subject_ID and c.WebDisplay = 1
                                            order by p.ProgramID";


            string ProgramCoursesSqlStr =
                @"select p.programid, p.Title as ProgramName, c.CourseID, c.titlelong as CourseTitle, c.name as Code
                                            from Program p join MajorProgram mp on p.ProgramID = mp.ProgramID and p.WebDisplay = 1
                                            join MajorSubject ms on ms.MajorID = mp.MajorID
                                            join Subject s on s.SubjectID = ms.SubjectID
                                            join Course c on c.Subject = s.Subject_ID and c.WebDisplay = 1
                                            order by p.ProgramID";
                                            
            DataTable CoursesData = storeData(ProgramCoursesSqlStr);

            try
            {
                // open SQL connection and execute query string
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    SqlCommand command = new SqlCommand(ProgramMainSqlStr, connection);
                    connection.Open();
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        // read each line SQL returns and writes it to XML
                        while (reader.Read())
                        {
                            //DateTime updatedate = Convert.ToDateTime(reader["LastUD"]);
                            XElement ProgramNode = new XElement("Program");
                            ProgramNode.RemoveAll();
                            ProgramNode.Add(new XElement("ProgramID", reader["programid"]));
                            ProgramNode.Add(new XElement("ProgramName", reader["ProgramName"]));

                            DataRow[] CoursesRow;
                            CoursesRow = CoursesData.Select("programid = '" + reader["programid"] + "'");

                            if (CoursesRow.Length != 0)
                            {
                                XElement CoursesNode = new XElement("Courses");
                                int m = 0;
                                foreach (var x in CoursesRow)
                                {
                                    XElement CourseNode = new XElement("Course");
                                    CourseNode.Add(new XElement("CourseID", CoursesRow[m]["CourseID"].ToString()));
                                    CourseNode.Add(new XElement("CourseTitle",
                                        CoursesRow[m]["CourseTitle"].ToString()));
                                    CourseNode.Add(new XElement("Code", CoursesRow[m]["Code"].ToString()));
                                    m++;
                                    CoursesNode.Add(CourseNode);
                                }

                                ProgramNode.Add(CoursesNode);
                            }

                            MainCourseSection.Add(ProgramNode);
                        }

                        doc.Root.Add(MainCourseSection);
                        doc.Save(ProgramCoursefile);
                        string lines = "Something's wrong";

                        string valid = ValidateXML("ProgramCourse.xml", "ProgramCourse.xsd");
                        if (valid != "1")
                        {
                            lines = dateT + ": ProgramCourse.xml Failed. " + valid;
                        }
                        else
                        {
                            lines = dateT +
                                    ": ProgramCourse.xml - Successfully completed XML build and validation sequence.";
                        }

                       
                        writelog(lines, CMSlogs);

                        Console.WriteLine("CMS ProgramCourse.xml package complete.");
                    }
                }
            }
            catch (Exception e)
            {
                string lines = dateT + ": ProgramCourse.xml Failed. " + e.Message.ToString();
                writelog(lines, CMSlogs);
                Console.Write(e.Message.ToString());
                Console.Beep();
            }
        }

        #endregion

        // Utility methods

        public static string GetLawURLs(string username)
        {

            SpecialProfessors LawProfiles = new SpecialProfessors();
            List<SpecialProfessors> LawProfessors = new List<SpecialProfessors>();
                
            LawProfessors.Add(new SpecialProfessors() { ProfileURL = "https://lawURL.edu/user1", Username = "Username1" });
            LawProfessors.Add(new SpecialProfessors() { ProfileURL = "https://lawURL.edu/user2", Username = "Username2" });
            LawProfessors.Add(new SpecialProfessors() { ProfileURL = "https://lawURL.edu/user3", Username = "Username3" });
            LawProfessors.Add(new SpecialProfessors() { ProfileURL = "https://lawURL.edu/user4", Username = "Username4" });
            LawProfessors.Add(new SpecialProfessors() { ProfileURL = "https://lawURL.edu/user5", Username = "Username5" });
           
           
            return LawProfiles.GetUrl(username, LawProfessors);
        }

        public static string GetDicipline(string schoolid, string department, string title)
        {
            // First phase
            switch (schoolid)
            {
                case "LW":
                    return "Law";

                case "MD":
                    return "Medicine";

                case "NU":
                    return "Nursing";

                case "ED":
                    return "Education";
            }

            // second set of logic
            if (schoolid == "BU" && department == "Health Care Mgmt &amp; Org Leader")
                return "Management";
            if (schoolid == "LA" && department == "Modern Languages, Literatures, and Cultures")
                return "Modern Languages";
            if (schoolid == "EGR" && department == "ECSCI Computer Science")
                return "Computer Science";
            if (schoolid == "HS" && department == "Athletic Training/Sports Med.")
                return "Athletic Training &amp; Sports Medicine";

            // Third set. 

            if (schoolid == "LA" && department == "Sociology, Criminal Justice, and Anthropology" &&
                title.Contains("Anthropology"))
                return "Anthropology";
            if (schoolid == "LA" && department == "Sociology, Criminal Justice, and Anthropology" &&
                title.Contains("Criminal Justice"))
                return "Criminal Justice";
            if (schoolid == "LA" && department == "Sociology, Criminal Justice, and Anthropology" &&
                title.Contains("Sociology"))
                return "Sociology";

            if (schoolid == "LA" && department == "Visual and Performing Arts" &&
                title.Contains("Game Design & Development"))
                return "Game Design & Development";

            if (schoolid == "LA" && department == "Visual and Performing Arts" && title.Contains("Theater"))
                return "Theater";

            if (schoolid == "LA" && department == "Visual and Performing Arts" && title.Contains("Music"))
                return "Music";

            if (schoolid == "LA" && department == "Visual and Performing Arts" && title.Contains("Fine Arts"))
                return "Fine Arts";

            if (schoolid == "BU" && department == "Accounting" && title.Contains("Accounting"))
                return "Accounting";

            if (schoolid == "BU" && department == "Accounting" && title.Contains("Business Law"))
                return "Business Law";

            // FOURTH SET
            if (schoolid == "BU" && department == "Computer Information Systems") return department;
            if (schoolid == "BU" && department == "Entrepreneurship & Strategy") return department;
            if (schoolid == "BU" && department == "Finance") return department;
            if (schoolid == "BU" && department == "GAME Forum") return department;
            if (schoolid == "BU" && department == "International Business") return department;
            if (schoolid == "BU" && department == "Management") return department;
            if (schoolid == "BU" && department == "Marketing") return department;
            if (schoolid == "CO" && department == "Film Television and Media Arts") return department;
            if (schoolid == "CO" && department == "Interactive Media and Design") return department;
            if (schoolid == "CO" && department == "Journalism") return department;
            if (schoolid == "CO" && department == "Media Studies") return department;
            if (schoolid == "CO" && department == "Strategic Communication") return department;
            if (schoolid == "EGR" && department == "Civil Engineering") return department;
            if (schoolid == "EGR" && department == "Cybersecurity") return department;
            if (schoolid == "EGR" && department == "Industrial Engineering") return department;
            if (schoolid == "EGR" && department == "Mechanical Engineering") return department;
            if (schoolid == "EGR" && department == "Software Engineering") return department;
            if (schoolid == "HS" && department == "Biomedical Sciences") return department;
            if (schoolid == "HS" && department == "Diagnostic Imaging") return department;
            if (schoolid == "HS" && department == "Occupational Therapy") return department;
            if (schoolid == "HS" && department == "Physical Therapy") return department;
            if (schoolid == "HS" && department == "Physician Assistant") return department;
            if (schoolid == "HS" && department == "Social Work") return department;
            if (schoolid == "LA" && department == "Biological Sciences") return department;
            if (schoolid == "LA" && department == "Chemistry") return department;
            if (schoolid == "LA" && department == "Economics") return department;
            if (schoolid == "LA" && department == "English") return department;
            if (schoolid == "LA" && department == "History") return department;
            if (schoolid == "LA" && department == "Legal Studies") return department;
            if (schoolid == "LA" && department == "Mathematics") return department;
            if (schoolid == "LA" && department == "Philosophy") return department;
            if (schoolid == "LA" && department == "Physics") return department;
            if (schoolid == "LA" && department == "Political Science") return department;
            if (schoolid == "LA" && department == "Psychology") return department;

            return "N/A"; // department;
        }

        #region Write log - write the log file

        public static void writelog(string _msg, string _path)
        {
            logfiletext = logfiletext + "\r\n" + _msg;

            if (!File.Exists(_path))
            {
                using (StreamWriter sw = File.CreateText(_path))
                {
                    sw.WriteLine(_msg);
                    return;
                }
            }

            using (StreamWriter sw = File.AppendText(_path))
            {
                sw.WriteLine(_msg);
                return;
            }
        }

        #endregion

        #region Clean  line by line writer from temppath to real path

        public static void cleanfile(string _tempPath, string _Path)
        {
            using (StreamReader Reader = new StreamReader(_tempPath))
            {
                using (StreamWriter Writer = new StreamWriter(_Path))
                {
                    int LineNumber = 0;
                    while (!Reader.EndOfStream)
                    {
                        string Line = Reader.ReadLine();
                        Writer.WriteLine(ReplaceLine(Line, LineNumber++));
                    }
                }
            }
        }

        #endregion

        #region replaceline() fixes a number of issues in Old Faculty profile xml/html

        public static string ReplaceLine(string Line, int LineNumber)
        {
            Line = Line
                .Replace("&amp;&amp;", "&amp;")
                .Replace("&amp;amp;", "&amp;")
                .Replace("[", "%5b")
                .Replace("]", "%5d")
                .Replace("&#x1B;[D", @"")
                .Replace("&lt;data", "<data")
                .Replace("&lt;/data&gt;", "</data>")
                .Replace("&lt;bio", "<bio")
                .Replace("&lt;/bio", "</bio>")
                .Replace("&lt;sections", "<sections")
                .Replace("&lt;/sections", "</sections>")
                .Replace("&lt;section", "<section")
                .Replace("&lt;/section", "</section>")
                .Replace("&lt;title", "<title")
                .Replace("&lt;/title", "</title>")
                .Replace("&lt;desc", "<desc")
                .Replace("&lt;/desc", "</desc>")
                .Replace("&lt;files", "<files")
                .Replace("&lt;/files", "</files>")
                .Replace("&lt;file", "<file")
                .Replace("&lt;/file", "</file>")
                .Replace("&lt;fname", "<fname")
                .Replace("&lt;/fname", "</fname>")
                .Replace("&lt;fdesc", "<fdesc")
                .Replace("&lt;/fdesc", "</fdesc>")
                .Replace("&lt;ftitle", "<ftitle")
                .Replace("&lt;/ftitle", "</ftitle>")
                .Replace("&gt;", ">")
                .Replace(">>", ">")
                .Replace("&amp;gt;", "&gt;") // new
                .Replace("&amp;lt;", "&lt;") // new
                .Replace(@"<fname>http://</fname>", @"<fname>http://www.URL.edu/x65.xml</fname>");


            return Line;
        }

        #endregion

        #region SimpleXMLWrite -- writes a xml via dataset. Not used** 

        public static string simplexmlwrite(string sqlstr, string CourseFile, string theroot)
        {
            string tempfile = CMSpath + @"tempCS.xml";
            string tempfile2 = @"C:\tempCS1.xml";


            try
            {
                String sConnection = ConfigurationManager.ConnectionStrings["DB"].ConnectionString;
                using (SqlConnection mySqlConnection = new SqlConnection(sConnection))
                {
                    mySqlConnection.Open();

                    // Get the same data through the provider.
                    SqlDataAdapter mySqlDataAdapter = new SqlDataAdapter(sqlstr, sConnection);
                    DataSet myDataSet2 = new DataSet();

                    myDataSet2.DataSetName = theroot;

                    mySqlDataAdapter.Fill(myDataSet2);

                    myDataSet2.WriteXml(tempfile);

                    //cleanfile(tempfile, tempfile2);
                    XDocument xdoc = XDocument.Load(tempfile);
                    xdoc.Element(theroot).Add(new XAttribute("Date_Exported",
                        DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz")));

                    xdoc.Save(CourseFile);

                    File.Delete(tempfile);

                    mySqlConnection.Close();
                    return DateTime.Now.ToString() + ": Successfully Wrote " + CourseFile;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message.ToString());
                return DateTime.Now.ToString() + ": Failed on " + CourseFile + " - " + e.Message.ToString();
            }
        }

        #endregion

        #region storeData method, creates and returns a datatable of sql table.

        public static DataTable storeData(string sqlcmd)
        {
            try
            {
                String sConnection = ConfigurationManager.ConnectionStrings["DB"].ConnectionString;
                SqlConnection mySqlConnection = new SqlConnection(sConnection);
                mySqlConnection.Open();
                SqlDataAdapter mySqlDataAdapter = new SqlDataAdapter(sqlcmd, sConnection);
                DataTable myDataSet2 = new DataTable();
                mySqlDataAdapter.Fill(myDataSet2);
                return myDataSet2;
            }
            catch (Exception e)
            {
                DataTable blank = new DataTable();
                return blank;
            }
        }

        #endregion

        #region Validate XML - validates based on schema file

        public static string ValidateXML(string XMLFile, string SchemaFile)
        {
            XmlReaderSettings xmlSettings = new XmlReaderSettings();
            xmlSettings.Schemas.Add("", XMLSchemaDir + SchemaFile);
            xmlSettings.ValidationType = ValidationType.Schema;
            XmlReader xmlInput = XmlReader.Create(CMSpath + XMLFile, xmlSettings);
            try
            {
                while (xmlInput.Read())
                {
                }

                xmlInput.Close();
                return "1";
            }
            catch (Exception e)
            {
                //badxml + XMLFile;
                xmlInput.Close();
                if (File.Exists(badxml + XMLFile))
                    File.Delete(badxml + XMLFile);
                File.Move(CMSpath + XMLFile, badxml + "Failed-" + DateTime.Now.ToString("MM-dd-yy") + "-" + XMLFile);

                return e.Message.ToString();
            }
        }

        #endregion
    }


    public class SpecialProfessors
    {
        public string Username { get; set; }
        public string ProfileURL { get; set; }

        public List<SpecialProfessors> LawProfiles { get; set; }
        
        
        public void InitiateList()
        {
            
            

        }
        
        
        public string GetUrl(string user, List<SpecialProfessors> Law) 
        {
            SpecialProfessors result = Law.Find(x => x.Username == user.ToLower());
            
            try{
                return result.ProfileURL; 
            }
            catch{
                return "0";
            }
        }
    }
}
