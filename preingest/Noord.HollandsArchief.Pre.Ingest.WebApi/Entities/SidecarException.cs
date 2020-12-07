using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace Noord.HollandsArchief.Pre.Ingest.WebApi.Entities
{
    [Serializable]
    public class SidecarException : Exception
    {
        private string _message = string.Empty;
        private Exception _exception = null;
        public SidecarException(SerializationInfo info, StreamingContext context): base(info, context)
        {
        }

        public SidecarException(String message) : base(message) { _message = message; }
        public SidecarException(Exception exception) : base(exception.Message, exception)
        {
            _exception = exception;
            _message = exception.Message;
            if (string.IsNullOrEmpty(_message))
                _message = (exception.InnerException != null) ? exception.InnerException.Message : (exception.GetBaseException() != null) ? exception.GetBaseException().Message : exception.GetBaseException().StackTrace;
        }

        public override String Message
        {
            get
            {
                bool isEmpty = String.IsNullOrEmpty(_message);
                if (!isEmpty)
                    return this._message;

                return _message = (_exception.InnerException != null) ? _exception.InnerException.Message : (_exception.GetBaseException() != null) ? _exception.GetBaseException().Message : _exception.GetBaseException().StackTrace;
            }
        }
    }
}
