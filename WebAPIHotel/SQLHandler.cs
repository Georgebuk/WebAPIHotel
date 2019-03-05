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
                        switch(type)
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

        public string executePost(RequestType type, Object objectToPost)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                switch (type)
                {
                    case RequestType.HOTEL_POST_REQUEST:
                        return null;
                    case RequestType.BOOKING_POST_REQUEST:
                        return addBooking(connection, objectToPost);
                }
            }
            return "fail";
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
        private string addBooking(SqlConnection connection, Object objectToPost)
        {
            //Setup the insert command with parameters
            string cmdString = "INSERT INTO cust_booking(cust_id, date_booking_made, date_for_booking, booking_finish_date, booking_activated, hide_booking, hotel_id, room_ID, qrcode_guid) " +
                "VALUES(@custID, @dateBookingMade, @dateForBooking, @BookingFinishDate, @BookingActivated, @HideBooking, @hotelID, @roomID, @QRCode);";
            
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
                    sqlCommand.Parameters.AddWithValue("@hotelID", booking.Hotel.HotelID);
                    sqlCommand.Parameters.AddWithValue("@roomID", roomID);
                    //Generate unique ID for the QRCode, this will be converted into a bitmap in the phone app
                    sqlCommand.Parameters.AddWithValue("@QRCode", Guid.NewGuid().ToString());

                    sqlCommand.ExecuteNonQuery();
                    return "success";
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("ERROR WHEN ADDING NEW BOOKING {0}", ex.Message);
                return "fail";
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
                    PricePerDay = float.Parse(reader["price_per_day"].ToString()),
                    IsAvailable = (bool)reader["is_room_available"]
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
            string cmdString = "SELECT * FROM hotel INNER JOIN HOTEL_FLOOR ON hotel.hotel_id = HOTEL_FLOOR.hotel_id INNER JOIN ROOM ON ROOM.hotel_floor_ID = HOTEL_FLOOR.hotel_floor_id WHERE hotel.hotel_id = {0}";
            

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
                    Activated = (bool)reader["booking_activated"],
                    HideBooking = (bool)reader["hide_booking"],
                    QrcodeString = reader["qrcode_guid"].ToString()
                };
                //int hotelID = Convert.ToInt32(reader["hotel_id"]);
                //var hotel = hotels.Find(h => h.HotelID == hotelID);
                //if (hotel == null)
                //{
                //    cmdString = String.Format(cmdString, hotelID);
                //    var gotHotel = execute(cmdString, RequestType.HOTEL_GET_REQUEST).Cast<Hotel>().ToList().ElementAt(0);
                //    hotels.Add(gotHotel);
                //    b.Hotel = gotHotel;
                //}
                //else
                //    b.Hotel = hotel;
                bookings.Add(b);
            }
            return bookings;
        }

        public string checkQRString(string QRString)
        {
            string QUERY = "SELECT * FROM cust_booking WHERE qrcode_guid = @QRString";
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (SqlCommand sqlCommand = new SqlCommand(QUERY, connection))
                {
                    sqlCommand.Parameters.AddWithValue("@QRString", QRString);
                    using (SqlDataReader reader = sqlCommand.ExecuteReader())
                    {
                        if (reader.HasRows)
                            return "success";
                        else
                            return "fail";
                    }
                }
            }
        }
    }
}
