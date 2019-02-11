using HotelClassLibrary;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
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

        public List<Object> execute(string cmdString, string requestType)
        {
            List<Object> objects = new List<object>();
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (SqlCommand sqlCommand = new SqlCommand(cmdString, connection))
                {
                    using (SqlDataReader reader = sqlCommand.ExecuteReader())
                    {
                        switch(requestType)
                        {
                            case "Bookings":
                                return getBookings(reader);
                                break;
                        }
                       
                    }
                }
            }
            return null;
        }

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
