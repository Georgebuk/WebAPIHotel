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

        private string addBooking(SqlConnection connection, Object objectToPost)
        {
            string cmdString = "INSERT INTO cust_booking(cust_id, date_booking_made, date_for_booking, booking_activated, hide_booking, hotel_id, room_ID) " +
                "VALUES(@custID, @dateBookingMade, @dateForBooking, @BookingActivated, @HideBooking, @hotelID, @roomID)";
            Booking booking = (Booking)objectToPost;
            try
            {
                using (SqlCommand sqlCommand = new SqlCommand(cmdString, connection))
                {
                    //Parameterize query to stop SQL Injection
                    sqlCommand.Parameters.AddWithValue("@custID", booking.Customer.CustId);
                    sqlCommand.Parameters.AddWithValue("@dateBookingMade", booking.DateBookingMade);
                    sqlCommand.Parameters.AddWithValue("@dateForBooking", booking.DateOfBooking);
                    sqlCommand.Parameters.AddWithValue("@BookingActivated", 0);
                    sqlCommand.Parameters.AddWithValue("@HideBooking", 0);
                    sqlCommand.Parameters.AddWithValue("@hotelID", booking.Hotel.HotelID);
                    sqlCommand.Parameters.AddWithValue("@roomID", booking.BookedRoom.RoomID);

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

                //Create Floor from reader
                Floor f = new Floor
                {
                    FloorID = Convert.ToInt32(reader["hotel_floor_ID"]),
                    FloorNumber = Convert.ToInt32(reader["floor_number"])
                };
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
                    //Add Room to the floor
                    f.RoomsOnFloor.Add(r);
                    //Add Floor to the hotel
                    hot.FloorsInHotel.Add(f);
                    //Finally add the hotel to the list of hotels
                    hotels.Add(hot);
                }
                //Hotel does exist
                else
                {
                    //Check if floor already exists in a hotel
                    Floor floor = h.FloorsInHotel.Find(flo => flo.FloorID == f.FloorID);
                    //If the floor does not already exist
                    if (floor == null)
                    {
                        //Add room to the floor
                        f.RoomsOnFloor.Add(r);
                        //Add floor to the hotel
                        h.FloorsInHotel.Add(f);
                    }
                    //If the floor does exist
                    else
                    {
                        //Add the room to the floor
                        floor.RoomsOnFloor.Add(r);
                    }
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
                    //Hotel = new Hotel
                    //{
                    //    HotelID = Convert.ToInt32(reader["hotel_id"]),
                    //    HotelName = reader["hotel_name"].ToString(),
                    //    HotelPostcode = reader["hotel_postcode"].ToString(),
                    //    AddressLine1 = reader["hotel_address1"].ToString(),
                    //    AddressLine2 = reader["hotel_address2"].ToString(),
                    //    City = reader["hotel_city"].ToString(),
                    //},
                    BookedRoom = new Room
                    {
                        RoomID = Convert.ToInt32(reader["room_id"]),
                        RoomNumber = Convert.ToInt32(reader["room_number"]),
                        PricePerDay = float.Parse(reader["price_per_day"].ToString())
                    },
                    DateBookingMade = DateTime.Parse(reader["date_booking_made"].ToString()),
                    DateOfBooking = DateTime.Parse(reader["date_for_booking"].ToString()),
                    Activated = (bool)reader["booking_activated"],
                    HideBooking = (bool)reader["hide_booking"]
                };
                int hotelID = Convert.ToInt32(reader["hotel_id"]);
                var hotel = hotels.Find(h => h.HotelID == hotelID);
                if (hotel == null)
                {
                    cmdString = String.Format(cmdString, hotelID);
                    var gotHotel = execute(cmdString, RequestType.HOTEL_GET_REQUEST).Cast<Hotel>().ToList().ElementAt(0);
                    hotels.Add(gotHotel);
                    b.Hotel = gotHotel;
                }
                else
                    b.Hotel = hotel;
                bookings.Add(b);
            }
            return bookings;
        }
    }
}
