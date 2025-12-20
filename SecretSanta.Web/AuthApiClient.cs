using System;
using System.Collections.Generic;
using System.Text;

namespace SecretSanta.Web;

    public class AuthApiClient(HttpClient httpClient)
    {
        public bool Auth(string login, string password)
        {
            return false;
        }

        public bool Register(string login, string password)
        {
            return false;
        }
    }

