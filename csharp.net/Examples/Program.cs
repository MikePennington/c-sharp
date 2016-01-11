using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Ploeh.AutoFixture;
using PubNubMessaging.Core;

namespace Examples
{
    public class Program
    {
        private const string SubscribeKey = "demo-36";
        private const string PublishKey = "demo-36";
        private static readonly string Channel = Guid.NewGuid().ToString();

        public static void Main(string[] args)
        {
            var fixture = new Fixture();
            var userCreatedEvent = fixture.Create<UserCreated>();
            
            var pubnub = new Pubnub2(SubscribeKey, PublishKey);

            pubnub.Subscribe<UserCreated>(Channel, SubscribeCallback<UserCreated>, ConnectCallback, PresenceCallback, ErrorCallback);

            Thread.Sleep(100);

            pubnub.Publish<UserCreated>(Channel, userCreatedEvent, PublishCallback, ErrorCallback);

            Console.ReadKey();
        }

        private static void SubscribeCallback<T>(Message<UserCreated> message)
        {
            Console.WriteLine("SubscribeCallback:\n" + message);
        }

        private static void ConnectCallback(Ack message)
        {
            Console.WriteLine("ConnectCallback: " + message);
        }

        private static void PresenceCallback(Ack message)
        {
            Console.WriteLine("PresenceCallback: " + message);
        }

        private static void ErrorCallback(PubnubClientError message)
        {
            Console.WriteLine("ErrorCallback: " + message);
        }

        private static void PublishCallback<T>(T message)
        {
            Console.WriteLine("PublishCallback: " + message);
        }

        private static void PublishCallbackObject(object message)
        {
            Console.WriteLine("PublishCallbackObject: " + message);
        }
    }

    public class UserCreated
    {
        public DateTime TimeStamp { get; set; }
        public User User { get; set; }
    }

    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public Address Address { get; set; }
        public List<Phone> Phones { get; set; }
    }

    public class Address
    {
        public Guid Id { get; set; }
        public string Street { get; set; }
    }

    public class Phone
    {
        public string Number { get; set; }
        public string Extenion { get; set; }
        public PhoneType PhoneType { get; set; }
    }

    public enum PhoneType
    {
        Home,
        Mobile,
        Work
    }
}
