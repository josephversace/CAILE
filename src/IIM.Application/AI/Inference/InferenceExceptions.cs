using System;
using System.Collections.Generic;
using System.Text;



namespace IIM.Application.Inference
{


    #region Exceptions


    /// <summary>
    /// Inference exception
    /// </summary>
    public class InferenceException : Exception
    {
        public InferenceException(string message) : base(message) { }
        public InferenceException(string message, Exception innerException) : base(message, innerException) { }
    }

    #endregion
}
