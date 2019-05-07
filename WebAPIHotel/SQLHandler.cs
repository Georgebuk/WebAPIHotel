using HotelClassLibrary;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace WebAPIHotel
{
    public class SQLHandler
    {
        private static string connectionString = "Data Source=(LocalDB)\\MSSQLLocalDB;AttachDbFilename=C:\\Users\\George\\source\\repos\\WebAPIHotel\\WebAPIHotel\\App_Data\\Database1.mdf;Integrated Security=True";
        public SQLHandler()
        { }

        //This method executes whatever query is passed to it from the controllers
        //Parsing the SQL Data into objects is then delegated to the appropriate method
        //depending upon the request type

        public List<Object> Execute(string cmdString, RequestType type)
        {
            List<Object> objects = new List<object>();
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (SqlCommand sqlCommand = new SqlCommand(cmdString, connection))
                {
                    using (SqlDataReader reader = sqlCommand.ExecuteReader())
                    {
                        switch (type)
                        {
                            case RequestType.BOOKING_REQUEST:
                                return GetBookings(reader);
                            case RequestType.HOTEL_GET_REQUEST:
                                return GetHotels(reader);

                        }

                    }
                }
            }
            return null;
        }
        //Method responsible for executing post requests, sorts by request type and takes the object to post
        //This method takes an object instead of a cmd string to allow parametrisation

        public ErrorEnum ExecutePost(RequestType type, Object objectToPost)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                switch (type)
                {
                    case RequestType.HOTEL_POST_REQUEST:
                        return ErrorEnum.USER_EXISTS_ERROR;
                    case RequestType.BOOKING_POST_REQUEST:
                        return AddBooking(connection, objectToPost);
                    case RequestType.ADD_NEW_USER_POST_REQUEST:
                        return RegisterNewUser(objectToPost);
                }
            }
            return ErrorEnum.INVALID_REQUEST_TYPE;
        }

        //Method responsible for finding and then assigning a room to a booking 
        //based on the speicified date range
        public int GetAvailableRoom(Booking booking)
        {
            //Initialise ID as 0, 0 means no room found
            int roomID = 0;
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                //Query to find rooms with bookings that overlap the specified dates requested
                string cmd = "SELECT * FROM cust_booking WHERE(cust_booking.date_for_booking <= '" + booking.BookingFinishDate.ToString("yyyy/MM/dd") + "' AND '" + booking.DateOfBooking.ToString("yyyy/MM/dd") + "' <= cust_booking.booking_finish_date)";
                List<int> bookedRooms = new List<int>();
                //Query to select a room that does not have a booking for the specified dates
                string getAvailableRoomCmd = "";
                using (SqlCommand sqlCMD = new SqlCommand(cmd, connection))
                {
                    using (SqlDataReader reader = sqlCMD.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            bookedRooms.Add(Convert.ToInt32(reader["room_ID"]));
                        }
                        if (bookedRooms.Count != 0)
                        {
                            getAvailableRoomCmd = "SELECT TOP 1 * FROM ROOM WHERE room_ID NOT IN(";
                            foreach (int i in bookedRooms)
                            {
                                if (i != bookedRooms.Last())
                                    getAvailableRoomCmd += i + ",";
                                else
                                    getAvailableRoomCmd += i + ")";
                            }
                            getAvailableRoomCmd += " AND hotel_id = " + booking.Hotel.HotelID + " AND IsAvailable = 1";
                        }
                        else
                            getAvailableRoomCmd += "SELECT TOP 1 * FROM ROOM WHERE hotel_ID =" + booking.Hotel.HotelID + " AND IsAvailable = 1";
                    }
                    using (SqlCommand cmd2 = new SqlCommand(getAvailableRoomCmd, connection))
                    {
                        SqlDataReader reader2 = cmd2.ExecuteReader();
                        //If there are any rooms available in the specified time frame
                        //get the room id
                        if (reader2.HasRows)
                        {
                            reader2.Read();
                            roomID = Convert.ToInt32(reader2["room_ID"]);
                        }
                    }
                }
            }
            return roomID;
        }

        //This method handles the post request for bookings
        //This method calls the getAvailableRooms method to assign a room to the booking
        private ErrorEnum AddBooking(SqlConnection connection, Object objectToPost)
        {
            //Setup the insert command with parameters
            string cmdString = "INSERT INTO cust_booking(cust_id, date_booking_made, date_for_booking, booking_finish_date, booking_activated, hide_booking, room_ID, qrcode_guid, booking_completed) " +
                "VALUES(@custID, @dateBookingMade, @dateForBooking, @BookingFinishDate, @BookingActivated, @HideBooking, @roomID, @QRCode, @Completed);";

            //Cast object to booking to access its properties
            Booking booking = (Booking)objectToPost;
            //Check the database for overlapping booking information and get available room
            int roomID = GetAvailableRoom(booking);

            try
            {
                using (SqlCommand sqlCommand = new SqlCommand(cmdString, connection))
                {
                    //Parameterize query to stop SQL Injection
                    sqlCommand.Parameters.AddWithValue("@custID", booking.Customer.CustId);
                    sqlCommand.Parameters.AddWithValue("@dateBookingMade", booking.DateBookingMade);
                    sqlCommand.Parameters.AddWithValue("@dateForBooking", booking.DateOfBooking);
                    sqlCommand.Parameters.AddWithValue("@BookingFinishDate", booking.BookingFinishDate);
                    sqlCommand.Parameters.AddWithValue("@BookingActivated", 0);
                    sqlCommand.Parameters.AddWithValue("@HideBooking", 0);
                    sqlCommand.Parameters.AddWithValue("@roomID", roomID);
                    //Generate unique ID for the QRCode, this will be converted into a bitmap in the phone app
                    sqlCommand.Parameters.AddWithValue("@QRCode", Guid.NewGuid().ToString());
                    sqlCommand.Parameters.AddWithValue("@Completed", 0);

                    sqlCommand.ExecuteNonQuery();
                    return ErrorEnum.SUCCESS;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("ERROR WHEN ADDING NEW BOOKING {0}", ex.Message);
                return ErrorEnum.BOOKING_FAILED;
            }

        }

        //This method gets hotels from the database and parses the response into objects
        //This is done as one query as it is much faster than several queries each getting the hotel then the floors
        //then the rooms
        private List<object> GetHotels(SqlDataReader reader)
        {
            List<Hotel> hotels = new List<Hotel>();

            while (reader.Read())
            {
                int HotelID = Convert.ToInt32(reader["hotel_id"]);

                //Check if hotel already exists in list
                Hotel h = hotels.Find(hotel => hotel.HotelID == HotelID);

                //Create Room from reader
                Room r = ConstructRoom(reader);
                //if the hotel does not already exist
                if (h == null)
                {
                    //Create a new Hotel
                    Hotel hot = new Hotel
                    {
                        HotelID = HotelID,
                        HotelName = reader["hotel_name"].ToString(),
                        HotelPostcode = reader["hotel_postcode"].ToString(),
                        AddressLine1 = reader["hotel_address1"].ToString(),
                        AddressLine2 = reader["hotel_address2"].ToString(),
                        City = reader["hotel_city"].ToString(),
                        ImageURL = @"@drawable/premierInn.jpg",
                        HotelDesc = reader["hotel_desc"].ToString()
                    };
                    //Add Room to the Hotel
                    hot.RoomsInHotel.Add(r);
                    //Finally add the hotel to the list of hotels
                    hotels.Add(hot);
                }
                //Hotel does exist
                else
                {
                    h.RoomsInHotel.Add(r);
                }

            }
            return hotels.Cast<Object>().ToList();
        }

        //This method gets all the customers bookings
        private List<Object> GetBookings(SqlDataReader reader)
        {
            List<Object> bookings = new List<Object>();
            List<Hotel> hotels = new List<Hotel>();

            while (reader.Read())
            {
                Booking b = new Booking
                {
                    BookingID = Convert.ToInt32(reader["booking_id"]),
                    Hotel = new Hotel
                    {
                        HotelID = Convert.ToInt32(reader["hotel_id"]),
                        HotelName = reader["hotel_name"].ToString(),
                        HotelPostcode = reader["hotel_postcode"].ToString(),
                        AddressLine1 = reader["hotel_address1"].ToString(),
                        AddressLine2 = reader["hotel_address2"].ToString(),
                        City = reader["hotel_city"].ToString(),
                        HotelDesc = reader["hotel_desc"].ToString()
                    },
                    BookedRoom = new Room
                    {
                        RoomID = Convert.ToInt32(reader["room_id"]),
                        RoomNumber = Convert.ToInt32(reader["room_number"]),
                        PricePerDay = float.Parse(reader["price_per_day"].ToString())
                    },
                    DateBookingMade = DateTime.Parse(reader["date_booking_made"].ToString()),
                    DateOfBooking = DateTime.Parse(reader["date_for_booking"].ToString()),
                    BookingFinishDate = DateTime.Parse(reader["booking_finish_date"].ToString()),
                    Activated = (bool)reader["booking_activated"],
                    HideBooking = (bool)reader["hide_booking"],
                    QrcodeString = reader["qrcode_guid"].ToString(),
                    Completed = (bool)reader["booking_completed"]
                };

                if(b.BookingFinishDate < DateTime.Today)
                {
                    MarkBookingAsCompleted(b.BookingID);
                    b.Completed = true;
                    if (b.Activated)
                        b.Activated = false;
                }
                else if(CheckDateInRange(b.DateOfBooking, b.BookingFinishDate))
                {
                    if (!CheckRoomIsAvailable(b.BookedRoom.RoomID))
                    {
                        int newRoom = GetAvailableRoom(b);
                         b.BookedRoom = UpdateBookingRoom(b, newRoom);
                    }
                }
 
                bookings.Add(b);
            }
            return bookings;
        }

        private Room UpdateBookingRoom(Booking b, int newRoom)
        {
            string QUERY = "UPDATE cust_booking SET room_ID = @NewRoom WHERE booking_id = @BookingID; " +
                "SELECT * FROM ROOM WHERE room_ID = @NewRoom"  ;
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (SqlCommand sqlCommand = new SqlCommand(QUERY, connection))
                {
                    sqlCommand.Parameters.AddWithValue("@NewRoom", newRoom);
                    sqlCommand.Parameters.AddWithValue("@BookingID", b.BookingID);

                    using (SqlDataReader reader = sqlCommand.ExecuteReader())
                    {
                        reader.Read();
                        return ConstructRoom(reader);
                    }
                }
            }
        }

        private bool CheckRoomIsAvailable(int roomID)
        {
            string QUERY = "SELECT IsAvailable FROM ROOM WHERE room_ID = @RoomID";
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (SqlCommand sqlCommand = new SqlCommand(QUERY, connection))
                {
                    sqlCommand.Parameters.AddWithValue("@RoomID", roomID);
                    using (SqlDataReader reader = sqlCommand.ExecuteReader())
                    {
                        reader.Read();
                        if (reader.HasRows)
                            return (bool)reader["IsAvailable"];
                        else
                            return false;
                    }
                }
            }
        }

        //Method to check if a scanned QR Code exists and is for the current booking date
        public string CheckQRString(string QRString)
        {
            string QUERY = "SELECT * FROM cust_booking WHERE qrcode_guid = @QRString";
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (SqlCommand sqlCommand = new SqlCommand(QUERY, connection))
                {
                    //Add QRString as a parameter
                    sqlCommand.Parameters.AddWithValue("@QRString", QRString);
                    using (SqlDataReader reader = sqlCommand.ExecuteReader())
                    {
                        reader.Read();
                        DateTime dateOfBooking = Convert.ToDateTime(reader["date_for_booking"]);
                        DateTime dateOfBookingEnd = Convert.ToDateTime(reader["booking_finish_date"]);
                        bool bookingDateValid = CheckDateInRange(dateOfBooking, dateOfBookingEnd);
                        int bookingID = Convert.ToInt32(reader["booking_id"]);
                        if (reader.HasRows)
                        {
                            if (!bookingDateValid)
                            {
                                if (dateOfBookingEnd < DateTime.Now)
                                {
                                    MarkBookingAsCompleted(bookingID);
                                    return "bookingEnded";
                                }
                                return "badDate";
                            }

                            MarkBookingAsActive(bookingID);
                            return "success" + Convert.ToInt32(reader["room_ID"]);
                        }
                        else
                        {
                            
                            return "fail";
                        }
                    }
                }
            }
        }

        private void MarkBookingAsActive(int bookingID)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                string QUERY = "UPDATE cust_booking SET booking_activated = 1 WHERE booking_id = " + bookingID;
                connection.Open();
                using (SqlCommand cmd = new SqlCommand(QUERY, connection))
                {
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void MarkBookingAsCompleted(int bookingID)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                string QUERY = "UPDATE cust_booking SET booking_completed = 1, booking_activated = 0 WHERE booking_id = " + bookingID + ";";
                connection.Open();
                using (SqlCommand cmd = new SqlCommand(QUERY, connection))
                {
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public bool Checkout(int bookingID)
        {
            //Complete the booking
            MarkBookingAsCompleted(bookingID);
            //Change the booking date to have finished earlier
            string QUERY = "UPDATE cust_booking SET booking_finish_date = @NewFinish WHERE booking_id = @BookingID;";
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (SqlCommand sqlCommand = new SqlCommand(QUERY, connection))
                {
                    sqlCommand.Parameters.AddWithValue("@NewFinish", DateTime.Now.Date);
                    sqlCommand.Parameters.AddWithValue("@BookingID", bookingID);
                    return true;
                }
            }
        }


        //Method checks if the the user is activating a booking within the correct dates
        private bool CheckDateInRange(DateTime startDate, DateTime endDate)
        {
            return DateTime.Now >= startDate && DateTime.Now < endDate;
        }

        //Methoid to re initilize the database for unit testing
        public bool DatabaseSetup()
        {
            string QUERY = "DROP TABLE IF EXISTS room_report;"
                + "DROP TABLE IF EXISTS ROOM;"
                + "DROP TABLE IF EXISTS hot_staff;"
                + "DROP TABLE IF EXISTS cust_booking;"
                + "DROP TABLE IF EXISTS hot_cust;"
                + "DROP TABLE IF EXISTS hotel;" +


                @"CREATE TABLE [dbo].[hot_cust] (
                [cust_id]          INT IDENTITY(1, 1) NOT NULL,
                [cust_first_name]  VARCHAR(255) NULL,
                [cust_last_name] VARCHAR(255) NOT NULL,
                [cust_email]       VARCHAR(255) NOT NULL,
                [cust_phonenumber] VARCHAR(15)  NULL,
                [cust_dob] DATE NOT NULL,
                [cust_password]    VARCHAR (48)  NOT NULL,
                PRIMARY KEY CLUSTERED([cust_id] ASC)
                );" +

                @"CREATE TABLE [dbo].[hotel] (
                [hotel_id]       INT IDENTITY(1, 1) NOT NULL,
                [hotel_name]     VARCHAR(255) NULL,
                [hotel_postcode] VARCHAR(10)  NOT NULL,
                [hotel_address1] VARCHAR(255) NULL,
                [hotel_address2] VARCHAR(255) NULL,
                [hotel_city] VARCHAR(100) NULL,
                [hotel_desc] VARCHAR(MAX) NULL,
                PRIMARY KEY CLUSTERED([hotel_id] ASC)
                );" +

                @"CREATE TABLE [dbo].[cust_booking] (
                [booking_id]          INT IDENTITY(1, 1) NOT NULL,
                [cust_id]             INT NOT NULL,
                [date_booking_made] DATETIME NOT NULL,
                [date_for_booking] DATE NOT NULL,
                [booking_activated] BIT DEFAULT((0)) NOT NULL,
                [hide_booking]        BIT DEFAULT((0)) NOT NULL,
                [room_ID]             INT NULL,
                [booking_finish_date] DATE NULL,
                [qrcode_guid]         VARCHAR(36) NULL,
                [booking_completed]   BIT DEFAULT((0)) NOT NULL,
                PRIMARY KEY CLUSTERED([booking_id] ASC),
                FOREIGN KEY([cust_id]) REFERENCES[dbo].[hot_cust] ([cust_id])
                );" +

                @"CREATE TABLE [dbo].[ROOM] (
                [room_ID]           INT IDENTITY(1, 1) NOT NULL,
                [room_number]       INT NOT NULL,
                [price_per_day] FLOAT(53) NULL,
                [hotel_ID]          INT NULL,
                [IsAvailable]   BIT        DEFAULT ((1)) NOT NULL,
                PRIMARY KEY CLUSTERED([room_ID] ASC),
                FOREIGN KEY([hotel_ID]) REFERENCES[dbo].[hotel] ([hotel_id])
                );" +

                @"CREATE TABLE [dbo].[hot_staff] (
	            [staff_id]         INT           IDENTITY (1, 1) NOT NULL,
	            [hotel_id]         INT           NOT NULL,
	            [staff_first_name] VARCHAR (255) NOT NULL,
	            [staff_last_name]  VARCHAR (255) NOT NULL,
	            [username]         VARCHAR (255) NOT NULL,
	            [staff_password]   VARCHAR (255) NULL,
	            PRIMARY KEY CLUSTERED ([staff_id] ASC),
	            FOREIGN KEY ([hotel_id]) REFERENCES [dbo].[hotel] ([hotel_id])
                );" +

                @"CREATE TABLE [dbo].[room_report] (
                [room_report_id] INT           IDENTITY (1, 1) NOT NULL,
                [room_ID]        INT           NOT NULL,
                [report]         VARCHAR (300) NOT NULL,
                [reporttime]     TIMESTAMP    NOT NULL,
                PRIMARY KEY CLUSTERED ([room_report_id] ASC),
                FOREIGN KEY ([room_ID]) REFERENCES [dbo].[ROOM] ([room_ID])
                );" +

                @"INSERT INTO hot_cust(cust_first_name, cust_last_name, cust_email, cust_phonenumber, cust_dob, cust_password) 
                VALUES ('George', 'Boulton', 'george.boulton@hotmail.co.uk', '07411762329', '1996/05/21', 'LHvGkIp870LnugAwmLYbeJgvbIAD8+kyZZkTJR4QIIPUWQ9j');" +

                @"INSERT INTO hot_cust(cust_first_name, cust_last_name, cust_email, cust_phonenumber, cust_dob, cust_password) 
                VALUES ('Johnathon', 'Smith', 'john.smith@jsmith.com', '07411762329', '1995/01/14', 'uXh1PNcPW1ucWC8sehgDaG/haGkrA3DeUnx6xLYDCzhZDwGL');" +

                @"INSERT INTO hotel(hotel_name, hotel_postcode, hotel_address1, hotel_address2, hotel_city, hotel_desc)
                VALUES('Hotel', 'ST4 2DU', '30 Avenue Road', '', 'Stoke-on-Trent', 'THIS IS A HOTEL IT IS VERY NICE');" +

                @"INSERT INTO hotel(hotel_name, hotel_postcode, hotel_address1, hotel_address2, hotel_city, hotel_desc)
                VALUES('A Different Hotel', 'ST4 2DP', '32 Avenue Road', '', 'Stoke-on-Trent', 'THIS IS A HOTEL IT IS ALSO VERY NICE');" +

                @"INSERT INTO cust_booking(cust_id, date_booking_made, date_for_booking, booking_activated, hide_booking, room_ID, booking_finish_date, qrcode_guid, booking_completed)
                VALUES(1, '2019/02/01', '2019/02/02', 1, 0, 1, '2019/02/27', '01d4e959-f983-4e33-a0e8-d753986bda1c', 0);" +

                @"INSERT INTO cust_booking(cust_id, date_booking_made, date_for_booking, booking_activated, hide_booking, room_ID, booking_finish_date, qrcode_guid, booking_completed)
                VALUES(1, '2019/02/01', '2019/05/21', 0, 0, 2, '2019/05/25', 'deb8ebc8-e251-40ea-bf20-78bf142d1dda', 0);" +

                @"INSERT INTO cust_booking(cust_id, date_booking_made, date_for_booking, booking_activated, hide_booking, room_ID, booking_finish_date, qrcode_guid, booking_completed)
                VALUES(1, '2019/01/01', '2019/01/11', 0, 0, 2, '2019/01/18', '6820e9da-617d-4382-9208-c3d883a84d8d', 1);" +

                @"INSERT INTO ROOM(room_number, hotel_ID, price_per_day) VALUES (101, 1, 30.5);
                INSERT INTO ROOM(room_number, hotel_ID, price_per_day, IsAvailable) VALUES(102, 1, 30.5, 0);
                INSERT INTO ROOM(room_number, hotel_ID, price_per_day, IsAvailable) VALUES(103, 1, 30.5, 0);
                INSERT INTO ROOM(room_number, hotel_ID, price_per_day) VALUES(104, 1, 30.5);
                INSERT INTO ROOM(room_number, hotel_ID, price_per_day) VALUES(201, 1, 30.5);
                INSERT INTO ROOM(room_number, hotel_ID, price_per_day) VALUES(202, 1, 30.5);
                INSERT INTO ROOM(room_number, hotel_ID, price_per_day) VALUES(203, 1, 30.5);
                INSERT INTO ROOM(room_number, hotel_ID, price_per_day) VALUES(204, 1, 30.5);

                INSERT INTO ROOM(room_number, hotel_ID, price_per_day) VALUES(101, 2, 30.5);
                INSERT INTO ROOM(room_number, hotel_ID, price_per_day) VALUES(102, 2, 30.5);
                INSERT INTO ROOM(room_number, hotel_ID, price_per_day) VALUES(103, 2, 30.5);
                INSERT INTO ROOM(room_number, hotel_ID, price_per_day) VALUES(104, 2, 30.5);
                INSERT INTO ROOM(room_number, hotel_ID, price_per_day) VALUES(201, 2, 30.5);
                INSERT INTO ROOM(room_number, hotel_ID, price_per_day) VALUES(202, 2, 30.5);
                INSERT INTO ROOM(room_number, hotel_ID, price_per_day) VALUES(203, 2, 30.5);
                INSERT INTO ROOM(room_number, hotel_ID, price_per_day) VALUES(204, 2, 30.5);" + 

                @"INSERT INTO hot_staff(hotel_id,staff_first_name, staff_last_name, username, staff_password)
                VALUES(1, 'George', 'Boulton', 'gb96', 'U8fCSoGfsg429oTcWOSMAsjMUF8jZhVmkKhwiZdfCMCZN5a8')";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (SqlCommand cmd = new SqlCommand(QUERY, connection))
                {
                    try
                    {
                        cmd.ExecuteNonQuery();
                    }
                    catch (Exception e)
                    {
                       string s = e.ToString();
                        return false;
                    }
                }
            }

            return true;
        }

        //Method to get user account from the database
        public Customer GetUser(string email, string password)
        {
            string QUERY = "SELECT * FROM hot_cust WHERE cust_email = @email";
            Customer customer = new Customer { CustId = 0 };
            //Open connection
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (SqlCommand command = new SqlCommand(QUERY, connection))
                {
                    //Add user details as Parameters
                    command.Parameters.AddWithValue("@email", email);

                    //Execute the command
                    SqlDataReader reader = command.ExecuteReader();
                    if (reader.HasRows)
                    {
                        reader.Read();

                        Customer c = new Customer
                        {
                            CustId = Convert.ToInt32(reader["cust_id"].ToString()),
                            First_name = reader["cust_first_name"].ToString(),
                            Last_name = reader["cust_last_name"].ToString(),
                            Email = reader["cust_email"].ToString(),
                            Phone_number = reader["cust_phonenumber"].ToString(),
                            dateOfBirth = DateTime.Parse(reader["cust_dob"].ToString()),
                            Password = reader["cust_password"].ToString()
                        };

                        bool UserVerified = PasswordVerifier.Verify(password, c.Password);
                        if(UserVerified)
                            return customer = c;

                    }
                }
            }
            return customer;
        }


        public Staff GetStaffMember(string username, string password)
        {
            string QUERY = "SELECT * FROM hot_staff WHERE username = @username";
            Staff staff = new Staff { StaffID = 0 };
            //Open connection
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (SqlCommand command = new SqlCommand(QUERY, connection))
                {
                    //Add user details as Parameters
                    command.Parameters.AddWithValue("@username", username);

                    //Execute the command
                    SqlDataReader reader = command.ExecuteReader();
                    if (reader.HasRows)
                    {
                        reader.Read();
                        int hotelID = Convert.ToInt32(reader["hotel_id"]);
                        Staff s = new Staff
                        {
                            StaffID = Convert.ToInt32(reader["staff_id"]),
                            FirstName = reader["staff_first_name"].ToString(),
                            LastName = reader["staff_last_name"].ToString(),
                            Username = reader["username"].ToString(),
                            Password = reader["staff_password"].ToString()
                        };

                        bool UserVerified = PasswordVerifier.Verify(password, s.Password);
                        if (UserVerified)
                        {
                            string hotelCommand = "SELECT * FROM hotel INNER JOIN ROOM ON ROOM.hotel_ID = HOTEL.hotel_id WHERE hotel.hotel_id = {0}";
                            hotelCommand = String.Format(hotelCommand, hotelID);
                            List<Object> hotels = Execute(hotelCommand, RequestType.HOTEL_GET_REQUEST);
                            s.Location = (Hotel)hotels[0];
                            return staff = s;
                        }

                    }
                }
            }
            return staff;
        }

        public ErrorEnum RegisterNewUser(Object customer)
        {
            Customer c = (Customer)customer;
            string registerQuery = "INSERT INTO hot_cust(cust_first_name, cust_last_name, cust_email, cust_phonenumber," +
                "cust_dob, cust_password) VALUES(@firstName, @lastName, @email," +
                "@phone, @dob, @password);";
            bool userAlreadyExists = UserExists(c.Email);
            if (userAlreadyExists)
                return ErrorEnum.USER_EXISTS_ERROR;

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (SqlCommand cmd = new SqlCommand(registerQuery, connection))
                {
                    cmd.Parameters.AddWithValue("@firstName", c.First_name);
                    cmd.Parameters.AddWithValue("@lastName", c.Last_name);
                    cmd.Parameters.AddWithValue("@email", c.Email);
                    cmd.Parameters.AddWithValue("@phone", c.Phone_number);
                    cmd.Parameters.AddWithValue("@dob", c.dateOfBirth);
                    cmd.Parameters.AddWithValue("@password", c.Password);

                    cmd.ExecuteNonQuery();
                    return ErrorEnum.SUCCESS;
                }
            }
        }

        private bool UserExists(string emailaddress)
        {
            string findUser = "SELECT * FROM hot_cust WHERE cust_email = @email";
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (SqlCommand cmd = new SqlCommand(findUser, connection))
                {
                    cmd.Parameters.AddWithValue("@email", emailaddress);
                    SqlDataReader reader = cmd.ExecuteReader();
                    return reader.HasRows;
                        
                }
            }
        }

        public bool BookRoomDown(int RoomID, string report)
        {
            string QUERY = "INSERT INTO room_report(room_ID, report) VALUES(@RoomID, @Report);"
                +"UPDATE ROOM SET IsAvailable = 0 WHERE room_ID = @RoomID";
            //Open connection
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (SqlCommand command = new SqlCommand(QUERY, connection))
                {
                    command.Parameters.AddWithValue("@RoomID", RoomID);
                    command.Parameters.AddWithValue("@Report", report);
                    if (command.ExecuteNonQuery() != 0)
                        return true;
                }
            }
            return false;
        }

        public bool SolveProblem(int RoomID)
        {
            string QUERY = "UPDATE ROOM SET IsAvailable = 1 WHERE room_id = @RoomID";
            //Open connection
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (SqlCommand command = new SqlCommand(QUERY, connection))
                {
                    command.Parameters.AddWithValue("@RoomID", RoomID);
                    if (command.ExecuteNonQuery() != 0)
                        return true;
                }
            }
            return false;
        }

        private Room ConstructRoom(SqlDataReader reader)
        {
            Room r = new Room
            {
                RoomID = Convert.ToInt32(reader["room_ID"]),
                RoomNumber = Convert.ToInt32(reader["room_number"]),
                PricePerDay = float.Parse(reader["price_per_day"].ToString()),
                IsAvailable = (bool)reader["IsAvailable"]
            };
            return r;
        }
    }
}
