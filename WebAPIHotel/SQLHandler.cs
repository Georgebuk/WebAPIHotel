using HotelClassLibrary;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
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
                            case RequestType.HOTEL_REQUEST:
                                return getHotels(reader);
                        }
                       
                    }
                }
            }
            return null;
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
            while (reader.Read())
            {
                Booking b = new Booking
                {
                    BookingID = Convert.ToInt32(reader["booking_id"]),
                    //Replace object instansiation with Flyweight or collection of all hotels/active customers
                    //Customer = new Customer
                    //{
                    //    CustId = Convert.ToInt32(reader["cust_id"]),
                    //    First_name = reader["cust_first_name"].ToString(),
                    //    Last_name = reader["cust_last_name"].ToString(),
                    //    Email = reader["cust_email"].ToString(),
                    //    Phone_number = reader["cust_phonenumber"].ToString(),
                    //    DateOfBirth = DateTime.Parse(reader["cust_dob"].ToString())
                    //},
                    Hotel = new Hotel
                    {
                        HotelID = Convert.ToInt32(reader["hotel_id"]),
                        HotelName = reader["hotel_name"].ToString(),
                        HotelPostcode = reader["hotel_postcode"].ToString(),
                        AddressLine1 = reader["hotel_address1"].ToString(),
                        AddressLine2 = reader["hotel_address2"].ToString(),
                        City = reader["hotel_city"].ToString(),
                    },
                    DateBookingMade = DateTime.Parse(reader["date_booking_made"].ToString()),
                    DateOfBooking = DateTime.Parse(reader["date_for_booking"].ToString()),
                    Activated = (bool)reader["booking_activated"],
                    HideBooking = (bool)reader["hide_booking"]
                };

                bookings.Add(b);
            }
            return bookings;
        }
    }
}
