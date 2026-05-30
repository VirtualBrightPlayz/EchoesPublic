using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class NotServerException : Exception
{
    public NotServerException() : base("Code execution can only be performed on the server.")
    {

    }

    public NotServerException(string message) : base(message)
    {

    }
}
