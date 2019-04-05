using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebAPIHotel
{
    public enum RequestType
    {
        HOTEL_GET_REQUEST,
        HOTEL_POST_REQUEST,
        BOOKING_REQUEST,
        BOOKING_POST_REQUEST,
        QR_STRING_CHECK_REQUEST,
        ADD_NEW_USER_POST_REQUEST
    }
}