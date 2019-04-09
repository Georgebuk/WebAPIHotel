using HotelClassLibrary;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using WebAPIHotel;
using WebAPIHotel.Controllers;

namespace UnitTestService
{
    [TestClass]
    public class UnitTests
    {
        private static string connectionString = "Data Source=(LocalDB)\\MSSQLLocalDB;AttachDbFilename=C:\\Users\\George\\source\\repos\\WebAPIHotel\\WebAPIHotel\\App_Data\\Database1.mdf;Integrated Security=True";

        static SQLHandler handler = new SQLHandler();
        static Customer TestCustomer;
        static BookingController bookingController;
        static HotelController hotelController;
        static UserController userController;

        [ClassInitialize]
        public static void Setup(TestContext testContext)
        {
            handler.databaseSetup();
            TestCustomer = new Customer
            {
                CustId = 1,
                First_name = "George",
                Last_name = "Boulton",
                Email = "george.boulton@hotmail.co.uk",
                Password = "LHvGkIp870LnugAwmLYbeJgvbIAD8+kyZZkTJR4QIIPUWQ9j",
                Phone_number = "07411762329",
                Bookings = new List<Booking>()
            };
            bookingController = new BookingController();
            hotelController = new HotelController();
            userController = new UserController();
        }

        [TestMethod]
        public void GetBookingsForUserTest()
        {
            var bookingsString = bookingController.Get(TestCustomer.CustId);
            var bookings = JsonConvert.DeserializeObject<List<Booking>>(bookingsString);
            Booking booking1 = bookings[0];
            Assert.AreEqual(1, booking1.BookedRoom.RoomID);
            Assert.AreEqual("Premier Inn", booking1.Hotel.HotelName);
            Booking booking2 = bookings[1];
            Assert.AreEqual(2, booking2.BookedRoom.RoomID);
            Assert.AreEqual("Premier Inn", booking2.Hotel.HotelName);
        }

        [TestMethod]
        public void HotelGetHotelsTest()
        {
            var hotelsJSON = hotelController.Get();
            var hotels = JsonConvert.DeserializeObject<List<Hotel>>(hotelsJSON);
            Assert.AreEqual(2, hotels.Count);
            Hotel hotel1 = hotels[0];
            Assert.AreEqual(1, hotel1.HotelID);
            Assert.AreEqual("Premier Inn", hotel1.HotelName);
            Hotel hotel2 = hotels[1];
            Assert.AreEqual(2, hotel2.HotelID);
            Assert.AreEqual("A Different Premier Inn", hotel2.HotelName);
        }

        [TestMethod]
        public void AddBookingForUserTest()
        {
            DateTime date = DateTime.Today;
            //Create test booking
            Booking booking = new Booking
            {
                Customer = TestCustomer,
                Hotel = new Hotel { HotelID = 1 },
                DateBookingMade = date,
                DateOfBooking = date,
                BookingFinishDate = date.AddDays(5),
                Activated = false,
                HideBooking = false,
                QrcodeString = "a92a5376-1dbb-4135-9db8-01e54b3e673d"
            };

            //Save the booking
            bookingController.Post(booking);
            //Refresh booking data
            var bookingsString = bookingController.Get(TestCustomer.CustId);
            var bookings = JsonConvert.DeserializeObject<List<Booking>>(bookingsString);
            //Check if found booking is equal

            Assert.AreEqual(date, bookings[2].DateBookingMade);
            Assert.AreEqual(1, bookings[2].Hotel.HotelID);
            Assert.AreEqual(date.AddDays(5), bookings[2].BookingFinishDate);
        }

        [TestMethod]
        public void getUserTest()
        {
            Customer c = JsonConvert.DeserializeObject<Customer>(userController.Get(TestCustomer.Email, TestCustomer.Password));
            Assert.AreNotEqual(null, c);
            Assert.AreEqual(c.First_name, "George");
            Assert.AreEqual(c.Last_name, "Boulton");
            Assert.AreEqual(c.Phone_number, "07411762329");
        }

        [TestMethod]
        public void CreateNewUserTest()
        {
            Customer c = new Customer
            {
                First_name = "Test",
                Last_name = "Testington",
                Email = "test.test@test.com",
                Password = "Passw0rd",
                Phone_number = "07511732329",
                dateOfBirth = new DateTime(1990, 8, 21)
            };

            userController.Register(c);
            Customer c2 = null;
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                SqlCommand command = new SqlCommand("SELECT * FROM hot_cust WHERE cust_email = '" + c.Email + "'", connection);
                SqlDataReader reader = command.ExecuteReader();
                reader.Read();
                c2 = new Customer
                {
                    CustId = Convert.ToInt32(reader["cust_id"].ToString()),
                    First_name = reader["cust_first_name"].ToString(),
                    Last_name = reader["cust_last_name"].ToString(),
                    Email = reader["cust_email"].ToString(),
                    Phone_number = reader["cust_phonenumber"].ToString(),
                    dateOfBirth = DateTime.Parse(reader["cust_dob"].ToString()),
                    Password = reader["cust_password"].ToString()
                };
            }

            Assert.AreNotEqual(null, c2);
            Assert.AreEqual("Test", c2.First_name);
            Assert.AreEqual("Testington", c2.Last_name);
            Assert.AreEqual("07511732329", c2.Phone_number);
        }

        [TestMethod]
        public void CheckSeatsAreAvailableTest()
        {
            Booking booking = new Booking
            {
                Customer = new Customer { CustId = 1 },
                Hotel = new Hotel { HotelID = 1 },
                DateBookingMade = DateTime.Today,
                DateOfBooking = DateTime.Today.AddDays(1),
                BookingFinishDate = DateTime.Today.AddDays(5),
                Activated = false,
                QrcodeString = Guid.NewGuid().ToString()
            };
            Assert.AreEqual("True",hotelController.CheckAvailable(booking));
        }
    }
}
