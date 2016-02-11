﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
using PubNubMessaging.Core;
using System.Threading;
using NUnit.Framework;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Runtime.Serialization;

namespace PubNubMessaging.Tests
{
    public class UserCreated
    {
        public DateTime TimeStamp { get; set; }
        public User User { get; set; }
    }

    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public Addressee Addressee { get; set; }
        public List<Phone> Phones { get; set; }
    }

    public class Addressee
    {
        public Guid Id { get; set; }
        public string Street { get; set; }
    }

    public class Phone
    {
        public string Number { get; set; }
        public string Extenion { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public PhoneType PhoneType { get; set; }
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum PhoneType
    {
        [EnumMember(Value = "Home")]
        Home,
        [EnumMember(Value = "Mobile")]
        Mobile,
        [EnumMember(Value = "Work")]
        Work
    }

    [TestFixture]
    public class CustomGenerics
    {
        static Pubnub pubnub = null;
        ManualResetEvent mreDisconnect = new ManualResetEvent(false);
        ManualResetEvent mreConnect = new ManualResetEvent(false);
        ManualResetEvent mrePresence = new ManualResetEvent(false);
        ManualResetEvent mreSubscribe = new ManualResetEvent(false);
        ManualResetEvent mrePublish = new ManualResetEvent(false);
        ManualResetEvent mreGrant = new ManualResetEvent(false);

        int mreWaitTimeout = 310 * 1000;
        bool receivedMessage = false;

        public class MyCustomJsonNet : NewtonsoftJsonDotNet
        {
            public override T DeserializeToObject<T>(List<object> listObject)
            {
                T ret = default(T);
                if (typeof(T) == typeof(Message<UserCreated>))
                {
                    var message = new Message<UserCreated>
                    {
                        Time = Pubnub.TranslatePubnubUnixNanoSecondsToDateTime(listObject[1].ToString()),
                        ChannelName = (listObject.Count == 4) ? listObject[3].ToString() : listObject[2].ToString(),
                    };
                    
                    string json = pubnub.JsonPluggableLibrary.SerializeToJsonString(listObject[0]);
                    message.Data = pubnub.JsonPluggableLibrary.DeserializeToObject<UserCreated>(json);

                    ret = (T)Convert.ChangeType(message, typeof(Message<UserCreated>), CultureInfo.InvariantCulture);
                }
                else if (typeof(T) == typeof(GrantAck))
                {
                    Dictionary<string, object> grantDicObj = pubnub.JsonPluggableLibrary.ConvertToDictionaryObject(listObject[0]);
                    var ack = new GrantAck
                    {
                        StatusMessage = (grantDicObj != null) ? grantDicObj["message"].ToString() : "",
                        ChannelName = listObject[1].ToString(),
                        Type = typeof(T)
                    };
                    int statusCode = 0; //For Grant, status code 200 = success
                    if (grantDicObj != null)
                    {
                        if (int.TryParse(grantDicObj["status"].ToString(), out statusCode))
                            ack.StatusCode = statusCode;
                    }

                    ret = (T)Convert.ChangeType(ack, typeof(GrantAck), CultureInfo.InvariantCulture);
                }
                else if (typeof(T) == typeof(ConnectOrDisconnectAck))
                {
                    var ack = new ConnectOrDisconnectAck
                    {
                        StatusMessage = listObject[1].ToString(),
                        ChannelName = listObject[2].ToString(),
                        Type = typeof(T)
                    };
                    int statusCode;
                    if (int.TryParse(listObject[0].ToString(), out statusCode))
                        ack.StatusCode = statusCode;

                    ret = (T)Convert.ChangeType(ack, typeof(ConnectOrDisconnectAck), CultureInfo.InvariantCulture);
                }
                else if (typeof(T) == typeof(PublishAck))
                {
                    var ack = new PublishAck
                    {
                        StatusMessage = listObject[1].ToString(),
                        ChannelName = listObject[3].ToString(),
                        Type = typeof(T)
                    };
                    int statusCode;
                    if (int.TryParse(listObject[0].ToString(), out statusCode))
                        ack.StatusCode = statusCode;

                    ret = (T)Convert.ChangeType(ack, typeof(PublishAck), CultureInfo.InvariantCulture);
                }
                else if (typeof(T) == typeof(JoinOrLeaveAck))
                {
                    var presenceEvent = pubnub.JsonPluggableLibrary.ConvertToDictionaryObject(listObject[0]);
                    var ack = new JoinOrLeaveAck
                    {
                        Who = presenceEvent["uuid"].ToString(),
                        Status = presenceEvent["action"].ToString(),
                        HappendWhen = Pubnub.TranslatePubnubUnixNanoSecondsToDateTime(listObject[1].ToString()),
                        ChannelName = (listObject.Count == 4) ? listObject[3].ToString() : listObject[2].ToString(),
                        CountOfWho = Int32.Parse(presenceEvent["occupancy"].ToString()),
                        Type = typeof(T)
                    };

                    ret = (T)Convert.ChangeType(ack, typeof(JoinOrLeaveAck), CultureInfo.InvariantCulture);
                }
                else if (typeof(T) == typeof(Ack))
                {
                    var ack = new Ack
                    {
                        StatusMessage = listObject[1].ToString(),
                        ChannelName = listObject[2].ToString(),
                        Type = typeof(T)
                    };
                    int statusCode;
                    if (int.TryParse(listObject[0].ToString(), out statusCode))
                        ack.StatusCode = statusCode;

                    ret = (T)Convert.ChangeType(ack, typeof(Ack), CultureInfo.InvariantCulture);
                }
                else
                {
                    ret = (T)(object)listObject;
                }

                return ret;
            }

            public override T DeserializeToObject<T>(string jsonString)
            {
                T ret = default(T);
                bool isJsonObject = false;
                bool isJsonArray = false;

                JObject jsonObject = null;
                JArray jsonArray = null;

                try
                {
                    jsonObject = JObject.Parse(jsonString);
                    isJsonObject = true;
                }
                catch
                {
                    jsonArray = JArray.Parse(jsonString);
                    isJsonArray = true;
                }

                if (isJsonObject && typeof(T) == typeof(UserCreated))
                {
                    UserCreated userCreated = new UserCreated();
                    userCreated.TimeStamp = (DateTime)jsonObject["Data"]["TimeStamp"];
                    userCreated.User = pubnub.JsonPluggableLibrary.DeserializeToObject<User>(jsonObject["Data"]["User"].ToString());

                    ret = (T)Convert.ChangeType(userCreated, typeof(UserCreated), CultureInfo.InvariantCulture);
                }
                else if (isJsonObject && typeof(T) == typeof(User))
                {
                    User user = new User();
                    user.Id = (int)jsonObject["Id"];
                    user.Name = jsonObject["Name"].ToString();
                    user.Addressee = pubnub.JsonPluggableLibrary.DeserializeToObject<Addressee>(jsonObject["Addressee"].ToString());
                    user.Phones = pubnub.JsonPluggableLibrary.DeserializeToObject<List<Phone>>(jsonObject["Phones"].ToString());

                    ret = (T)Convert.ChangeType(user, typeof(User), CultureInfo.InvariantCulture);
                }
                else if (isJsonObject && typeof(T) == typeof(Addressee))
                {
                    Addressee addr = new Addressee();
                    addr.Id = Guid.Parse(jsonObject["Id"].ToString());
                    addr.Street = jsonObject["Street"].ToString();
                    ret = (T)Convert.ChangeType(addr, typeof(Addressee), CultureInfo.InvariantCulture);
                }
                else if (isJsonArray && typeof(T) == typeof(List<Phone>))
                {
                    List<Phone> phoneList = new List<Phone>();
                    foreach (JObject obj in jsonArray)
                    {
                        Phone phoneObj = pubnub.JsonPluggableLibrary.DeserializeToObject<Phone>(obj.ToString());
                        phoneList.Add(phoneObj);
                    }
                    ret = (T)Convert.ChangeType(phoneList, typeof(List<Phone>), CultureInfo.InvariantCulture);
                }
                else if (isJsonObject && typeof(T) == typeof(Phone))
                {
                    Phone ph = new Phone();
                    ph.Number = jsonObject["Number"].ToString();
                    ph.Extenion = jsonObject["Extenion"].ToString();
                    ph.PhoneType = (PhoneType)Enum.Parse(typeof(PhoneType), jsonObject["PhoneType"].ToString());

                    ret = (T)Convert.ChangeType(ph, typeof(Phone), CultureInfo.InvariantCulture);
                }
                return ret;
            }
        }

        [TestFixtureSetUp]
        public void Init()
        {
            if (!PubnubCommon.PAMEnabled) return;

            receivedMessage = false;

            pubnub = new Pubnub(PubnubCommon.PublishKey, PubnubCommon.SubscribeKey, PubnubCommon.SecretKey, "", false);
            pubnub.JsonPluggableLibrary = new MyCustomJsonNet();

            PubnubUnitTest unitTest = new PubnubUnitTest();
            unitTest.TestClassName = "GrantRequestUnitTest";
            unitTest.TestCaseName = "Init";
            pubnub.PubnubUnitTest = unitTest;

            string channel = "hello_my_channel";
            mreGrant = new ManualResetEvent(false);

            //Request Grant for Subscribe Channel
            pubnub.GrantAccess<GrantAck>(channel, true, true, ThenSubscribeInitializeShouldReturnGrantMessage, ErrorCallback);
            mreGrant.WaitOne();

            mreGrant = new ManualResetEvent(false);
            //Request Grant for Presence Channel (Presence Channel = Subscribe Channel + "-pnpres")
            pubnub.GrantPresenceAccess<GrantAck>(channel, true, true, ThenSubscribeInitializeShouldReturnGrantMessage, ErrorCallback);
            mreGrant.WaitOne();

            pubnub.EndPendingRequests();
            pubnub.PubnubUnitTest = null;
            pubnub = null;

            Assert.IsTrue(receivedMessage, "WhenSubscribedToAChannel Grant access failed.");
        }

        [Test]
        public void ThenChannelSubscribeShouldReturnPublishedMessage()
        {
            receivedMessage = false;

            pubnub = new Pubnub(PubnubCommon.PublishKey, PubnubCommon.SubscribeKey, "", "", false);

            //TO SUPPORT GENERICS, ENSURE THAT YOU IMPLEMENT "NewtonsoftJsonDotNet" METHODS FOR JSON DESERIALIZATION 
            pubnub.JsonPluggableLibrary = new MyCustomJsonNet();

            PubnubUnitTest unitTest = new PubnubUnitTest();
            unitTest.TestClassName = "WhenAClientIsPresented";
            unitTest.TestCaseName = "ThenPresenceShouldReturnReceivedMessage";
            pubnub.PubnubUnitTest = unitTest;

            mreWaitTimeout = (unitTest.EnableStubTest) ? 1000 : mreWaitTimeout;

            //WHEN USING BOTH PRESENCE AND SUBSCRIBE REQUESTS, FIRST DO PRESENCE THEN SUBSCRIBE
            string channel = "hello_my_channel";
            mrePresence = new ManualResetEvent(false);
            mreConnect = new ManualResetEvent(false);
            pubnub.Presence<UserCreated>(channel, "", SubscribeCallback, PresenceCallback, ConnectCallback, WildcardPresenceCallback, ErrorCallback);
            mreConnect.WaitOne(mreWaitTimeout);

            mreSubscribe = new ManualResetEvent(false);
            mreConnect = new ManualResetEvent(false);
            pubnub.Subscribe<Message<UserCreated>, JoinOrLeaveAck, ConnectOrDisconnectAck, JoinOrLeaveAck>(channel, "", SubscribeCallback, PresenceCallback, ConnectCallback, WildcardPresenceCallback, ErrorCallback);
            mreConnect.WaitOne(mreWaitTimeout);
            

            
            //Custom Object to be passed for Publish
            User user = new User();
            user.Addressee = new Addressee() { Id = Guid.NewGuid(), Street = "Test Street" };
            user.Id = 999;
            user.Name = "Pubnub";
            user.Phones = new List<Phone>() { new Phone() { Number = "111-222-3333", Extenion = "4444", PhoneType = PhoneType.Home }, new Phone() { Number = "999-888-7777", Extenion = "6666", PhoneType = PhoneType.Work } };

            Message<UserCreated> messageObject = new Message<UserCreated>();
            
            UserCreated userCreated = new UserCreated();
            userCreated.TimeStamp = DateTime.Now;
            userCreated.User = user;
            
            messageObject.Data = userCreated;

            mrePublish = new ManualResetEvent(false);
            //Console.WriteLine(pubnub.JsonPluggableLibrary.SerializeToJsonString(messageObject));
            pubnub.Publish<PublishAck>(channel, messageObject, PublishCallback, ErrorCallback);
            mrePublish.WaitOne(mreWaitTimeout);
            
            mreSubscribe.WaitOne(mreWaitTimeout);

            mreDisconnect = new ManualResetEvent(false);
            pubnub.Unsubscribe<Message<UserCreated>, JoinOrLeaveAck, ConnectOrDisconnectAck, JoinOrLeaveAck>(channel, "", SubscribeCallback, PresenceCallback, ConnectCallback, DisconnectCallback, WildcardPresenceCallback, ErrorCallback);
            mreDisconnect.WaitOne(mreWaitTimeout);

            mrePresence.WaitOne(mreWaitTimeout);

            pubnub.EndPendingRequests();
            pubnub.PubnubUnitTest = null;
            pubnub = null;

            Assert.IsTrue(receivedMessage, "Subscribe callback unable to receive the published message");
        }

        [Test]
        public void ThenChannelGroupSubscribeShouldReturnPublishedMessage()
        {
            receivedMessage = false;

            pubnub = new Pubnub(PubnubCommon.PublishKey, PubnubCommon.SubscribeKey, "", "", false);

            //TO SUPPORT GENERICS, ENSURE THAT YOU IMPLEMENT "NewtonsoftJsonDotNet" METHODS FOR JSON DESERIALIZATION 
            pubnub.JsonPluggableLibrary = new MyCustomJsonNet();

            PubnubUnitTest unitTest = new PubnubUnitTest();
            unitTest.TestClassName = "WhenAClientIsPresented";
            unitTest.TestCaseName = "ThenPresenceShouldReturnReceivedMessage";
            pubnub.PubnubUnitTest = unitTest;

            mreWaitTimeout = (unitTest.EnableStubTest) ? 1000 : mreWaitTimeout;

            //WHEN USING BOTH PRESENCE AND SUBSCRIBE REQUESTS, FIRST DO PRESENCE THEN SUBSCRIBE
            string channel = "";
            string channelGroup = "cg1";
            mrePresence = new ManualResetEvent(false);
            mreConnect = new ManualResetEvent(false);
            pubnub.Presence<UserCreated>(channel, channelGroup, SubscribeCallback, PresenceCallback, ConnectCallback, WildcardPresenceCallback, ErrorCallback);
            mreConnect.WaitOne(mreWaitTimeout);

            mreSubscribe = new ManualResetEvent(false);
            mreConnect = new ManualResetEvent(false);
            pubnub.Subscribe<Message<UserCreated>, JoinOrLeaveAck, ConnectOrDisconnectAck, JoinOrLeaveAck>(channel, channelGroup, SubscribeCallback, PresenceCallback, ConnectCallback, WildcardPresenceCallback, ErrorCallback);
            mreConnect.WaitOne(mreWaitTimeout);

            Thread.Sleep(2000);

            //Custom Object to be passed for Publish
            User user = new User();
            user.Addressee = new Addressee() { Id = Guid.NewGuid(), Street = "Test Street" };
            user.Id = 999;
            user.Name = "Pubnub";
            user.Phones = new List<Phone>() { new Phone() { Number = "111-222-3333", Extenion = "4444", PhoneType = PhoneType.Home }, new Phone() { Number = "999-888-7777", Extenion = "6666", PhoneType = PhoneType.Work } };

            Message<UserCreated> messageObject = new Message<UserCreated>();

            UserCreated userCreated = new UserCreated();
            userCreated.TimeStamp = DateTime.Now;
            userCreated.User = user;

            messageObject.Data = userCreated;

            mrePublish = new ManualResetEvent(false);
            channel = "ch1";
            //Console.WriteLine(pubnub.JsonPluggableLibrary.SerializeToJsonString(messageObject));
            pubnub.Publish<PublishAck>(channel, messageObject, PublishCallback, ErrorCallback);
            mrePublish.WaitOne(mreWaitTimeout);

            mreSubscribe.WaitOne(mreWaitTimeout);

            channel = "";
            channelGroup = "cg1";
            mreDisconnect = new ManualResetEvent(false);
            pubnub.Unsubscribe<Message<UserCreated>, JoinOrLeaveAck, ConnectOrDisconnectAck, JoinOrLeaveAck>(channel, channelGroup, SubscribeCallback, PresenceCallback, ConnectCallback, DisconnectCallback,  WildcardPresenceCallback, ErrorCallback);
            mreDisconnect.WaitOne(mreWaitTimeout);

            Thread.Sleep(6000); //Wait for channel group channels to unsub

            mrePresence.WaitOne(mreWaitTimeout);

            pubnub.EndPendingRequests();
            pubnub.PubnubUnitTest = null;
            pubnub = null;

            Assert.IsTrue(receivedMessage, "Subscribe callback unable to receive the published message");
        }
        
        private void SubscribeCallback(Message<UserCreated> message)
        {
            receivedMessage = true;
            Console.WriteLine("SubscribeCallback : " + pubnub.JsonPluggableLibrary.SerializeToJsonString(message));
            mreSubscribe.Set();
        }

        private void ConnectCallback(ConnectOrDisconnectAck ack)
        {
            Console.WriteLine("ConnectCallback: " + ack);
            mreConnect.Set();
        }

        private void DisconnectCallback(ConnectOrDisconnectAck ack)
        {
            Console.WriteLine("DisconnectCallback: " + ack);
            mreDisconnect.Set();
        }

        private void PresenceCallback(JoinOrLeaveAck ack)
        {
            Console.WriteLine("PresenceCallback: " + ack);
            mrePresence.Set();
        }

        private void WildcardPresenceCallback(JoinOrLeaveAck ack)
        {
            Console.WriteLine("PresenceCallback: " + ack);
        }

        private void ErrorCallback(PubnubClientError error)
        {
            Console.WriteLine("ErrorCallback: " + error);
        }
        private void PublishCallback(PublishAck ack)
        {
            Console.WriteLine("PublishCallback: " + ack);
            mrePublish.Set();
        }

        void ThenSubscribeInitializeShouldReturnGrantMessage(GrantAck ack)
        {
            try
            {
                if (ack != null && ack.StatusCode == 200)
                {
                    receivedMessage = true;
                }
            }
            catch { }
            finally
            {
                mreGrant.Set();
            }
        }
    }
    
}
