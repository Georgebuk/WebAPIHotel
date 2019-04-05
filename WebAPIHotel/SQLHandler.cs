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

        public List<Object> execute(string cmdString, RequestType type)
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
                                return getBookings(reader);
                            case RequestType.HOTEL_GET_REQUEST:
                                return getHotels(reader);

                        }

                    }
                }
            }
            return null;
        }
        //Method responsible for executing post requests, sorts by request type and takes the object to post
        //This method takes an object instead of a cmd string to allow parametrisation

        public ErrorEnum executePost(RequestType type, Object objectToPost)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                switch (type)
                {
                    case RequestType.HOTEL_POST_REQUEST:
                        return ErrorEnum.USER_EXISTS_ERROR;
                    case RequestType.BOOKING_POST_REQUEST:
                        return addBooking(connection, objectToPost);
                    case RequestType.ADD_NEW_USER_POST_REQUEST:
                        return RegisterNewUser(objectToPost);
                }
            }
            return ErrorEnum.INVALID_REQUEST_TYPE;
        }

        //Method responsible for finding and then assigning a room to a booking 
        //based on the speicified date range
        public int getAvailableRoom(Booking booking)
        {
            //Initialise ID as 0, 0 means no room found
            int roomID = 0;
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                //Query to find rooms with bookings that overlap the specified dates requested
                string cmd = "SELECT * FROM cust_booking WHERE(cust_booking.date_for_booking <= '" + booking.BookingFinishDate.ToString("yyyy/MM/dd") + "' AND '" + booking.DateOfBooking.ToString("yyyy/MM/dd") + "' <= cust_booking.booking_finish_date);";
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
                            getAvailableRoomCmd += " AND hotel_id = " + booking.Hotel.HotelID;
                        }
                        else
                            getAvailableRoomCmd += "SELECT TOP 1 * FROM ROOM WHERE hotel_ID =" + booking.Hotel.HotelID;
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
        private ErrorEnum addBooking(SqlConnection connection, Object objectToPost)
        {
            //Setup the insert command with parameters
            string cmdString = "INSERT INTO cust_booking(cust_id, date_booking_made, date_for_booking, booking_finish_date, booking_activated, hide_booking, room_ID, qrcode_guid) " +
                "VALUES(@custID, @dateBookingMade, @dateForBooking, @BookingFinishDate, @BookingActivated, @HideBooking, @roomID, @QRCode);";

            //Cast object to booking to access its properties
            Booking booking = (Booking)objectToPost;
            //Check the database for overlapping booking information and get available room
            int roomID = getAvailableRoom(booking);

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
        private List<object> getHotels(SqlDataReader reader)
        {
            List<Hotel> hotels = new List<Hotel>();

            while (reader.Read())
            {
                int HotelID = Convert.ToInt32(reader["hotel_id"]);

                //Check if hotel already exists in list
                Hotel h = hotels.Find(hotel => hotel.HotelID == HotelID);

                //Create Room from reader
                Room r = new Room
                {
                    RoomID = Convert.ToInt32(reader["room_ID"]),
                    RoomNumber = Convert.ToInt32(reader["room_number"]),
                    PricePerDay = float.Parse(reader["price_per_day"].ToString())
                };
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
        private List<Object> getBookings(SqlDataReader reader)
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
                    QrcodeString = reader["qrcode_guid"].ToString()
                };
 
                bookings.Add(b);
            }
            return bookings;
        }

        //Method to check if a scanned QR Code exists and is for the current booking date
        public string checkQRString(string QRString)
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
                        bool bookingDateValid = checkDateInRange(dateOfBooking, dateOfBookingEnd);
                        if (reader.HasRows && bookingDateValid)
                            return "success";
                        else
                            return "fail";
                    }
                }
            }
        }
        //Method checks if the the user is activating a booking within the correct dates
        private bool checkDateInRange(DateTime startDate, DateTime endDate)
        {
            return DateTime.Now >= startDate && DateTime.Now < endDate;
        }

        //Methoid to re initilize the database for unit testing
        public bool databaseSetup()
        {
            string QUERY = "DROP TABLE cust_booking;"
                + "DROP TABLE ROOM;"
                + "DROP TABLE hot_cust;"
                + "DROP TABLE hotel;" +


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
                PRIMARY KEY CLUSTERED([booking_id] ASC),
                FOREIGN KEY([cust_id]) REFERENCES[dbo].[hot_cust] ([cust_id])
                );" +

                @"CREATE TABLE [dbo].[ROOM] (
                [room_ID]           INT IDENTITY(1, 1) NOT NULL,
                [room_number]       INT NOT NULL,
                [price_per_day] FLOAT(53) NULL,
                [hotel_ID]          INT NULL,
                PRIMARY KEY CLUSTERED([room_ID] ASC),
                FOREIGN KEY([hotel_ID]) REFERENCES[dbo].[hotel] ([hotel_id])
                );" +

                @"INSERT INTO hot_cust(cust_first_name, cust_last_name, cust_email, cust_phonenumber, cust_dob) 
                VALUES('George', 'Boulton', 'george.boulton@hotmail.co.uk', '07411762329', '1996/05/21');" +

                @"INSERT INTO hotel(hotel_name, hotel_postcode, hotel_address1, hotel_address2, hotel_city, hotel_desc)
                VALUES('Premier Inn', 'ST4 2DU', '30 Avenue Road', '', 'Stoke-on-Trent', 'THIS IS A HOTEL IT IS VERY NICE');" +

                @"INSERT INTO hotel(hotel_name, hotel_postcode, hotel_address1, hotel_address2, hotel_city, hotel_desc)
                VALUES('A Different Premier Inn', 'ST4 2DP', '32 Avenue Road', '', 'Stoke-on-Trent', 'THIS IS A HOTEL IT IS ALSO VERY NICE');" +

                @"INSERT INTO cust_booking(cust_id, date_booking_made, date_for_booking, booking_activated, hide_booking, room_ID, booking_finish_date, qrcode_guid)
                VALUES(1, '2019/02/01', '2019/02/02', 0, 0, 1, '2019/02/27', '01d4e959-f983-4e33-a0e8-d753986bda1c');" +

                @"INSERT INTO cust_booking(cust_id, date_booking_made, date_for_booking, booking_activated, hide_booking, room_ID, booking_finish_date, qrcode_guid)
                VALUES(1, '2019/02/01', '2019/05/21', 0, 0, 2, '2019/02/27', 'deb8ebc8-e251-40ea-bf20-78bf142d1dda');" +

                @"INSERT INTO ROOM(room_number, hotel_ID, price_per_day) VALUES (101, 1, 30.5);
                INSERT INTO ROOM(room_number, hotel_ID, price_per_day) VALUES(102, 1, 30.5);
                INSERT INTO ROOM(room_number, hotel_ID, price_per_day) VALUES(103, 1, 30.5);
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
                INSERT INTO ROOM(room_number, hotel_ID, price_per_day) VALUES(204, 2, 30.5); ";

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
        public Customer getUser(string email, string password)
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

                        customer = new Customer
                        {
                            CustId = Convert.ToInt32(reader["cust_id"].ToString()),
                            First_name = reader["cust_first_name"].ToString(),
                            Last_name = reader["cust_last_name"].ToString(),
                            Email = reader["cust_email"].ToString(),
                            Phone_number = reader["cust_phonenumber"].ToString(),
                            dateOfBirth = DateTime.Parse(reader["cust_dob"].ToString()),
                            Password = reader["cust_password"].ToString()
                        };

                        bool UserVerified = PasswordVerifyer.Verify(password, customer.Password);
                        return customer;

                    }
                }
            }
            return customer;
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
    }
}
