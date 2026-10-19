using System;
using System.Collections.Generic;

namespace PostServiceFormsApp.Services
{
    public class ValidationErrorResponse
    {
        public Dictionary<string, string[]> Errors { get; set; } = new();
    }

    public class PostingValidationException : Exception
    {
        public Dictionary<string, string[]> Errors { get; }

        public PostingValidationException(Dictionary<string, string[]> errors)
            : base("Сервер відхилив дані відправлення.")
        {
            Errors = errors ?? new Dictionary<string, string[]>();
        }
    }
}
