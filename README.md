# This project has been retired.
I made this project quite a long time ago, and it has a few significant issues. 

Issues include **high idle CPU** usage and a **memory leak**.

Due to the poor quality spaghetti code of this project, I do not plan to fix it.

Instead, I plan to re-make an entirely new Socketing library to replace this. I just haven't decided on a name for it.

Once the new library is publicly available on GitHub, I will be archiving this project.

<br>
<br>
<br>
<br>
<br> 


# SnooperSocket
A .NET Socket/Message Broker designed to run through TCP.

Why is this project called "SnooperSocket", I don't really know. I was origonally developing this to use as a familiar socketing protocal to use along side with mono. But I decided to flush it out sp I could use it for some of my other projects.

# Usage

## Getting Started

the SnooperSocketClient is the core client for both server-side and client-side communication.

Call SnooperSococketClient.Start(); before sending or receiving data.
```cs
TcpClient tcpClient = new TcpClient();
tcpClient.Connect(IPAddress.Parse("127.0.0.1"), 2181);
SnooperSocketClient Client = new SnooperSocketClient(tcpClient);
Client.Start();
```

## Sending Data

SnooperSocket has 2 main means of data transmittion, Object and Raw Data. Raw data can be supplied as a stream or a byte array. Objects are serialized/deserialized using Newtonsoft.Json.

### Object Transmittion

```cs
MyData Data = new MyData() { Age = 21, Name = "David" };
Client.Write(Data);

public class MyData
{
    public string Name;
    public int Age;
}
```

This will send a message to the remote client with the supplied object.

### Raw Data Transmittion

To transfer raw data, there are 2 main methods to do so. Client.Write and Client.WriteRawData();

SnooperSocketClient.Write accepts an object, a stream, or a byte array. Using this method also sends some basic information for the remote server, such as message type and channel. 

Sending data using SnooperSocketClient.WriteRawData(); will only write the data in the SnooperSocket compatable format, as writing data directly to the TCP client will de-sync the remote client. This method only has an overhead of 5 bytes per message.

```cs
byte[] Payload = new byte[] { 21, 22, 23, 24, 25 };
Client.Write(Payload);
```

```cs
Client.WriteRawData(Payload);
```


## Receiving Data

On the most basic level, you will receive messages from the MessageRecieved Event on SnooperSocketClient. Messages from this are complete, so you don't need to worry about receiving multiple messages for a single sent message.

Usage:

This example displays reading an object from a request. This reads the message as a 'MyData' class. See object transmission above.

In practive you would rely mroe upon channels to determine object types, rather than SnooperMessage.ObjectType.

```cs
Client.MessageRecieved += Client_MessageRecieved;

private static void Client_MessageRecieved(SnooperMessage Message)
{
    if (Message.ObjectType == "MyData")
    {
        MyData Data = Message.ReadObject<MyData>();
        Console.WriteLine($"Name: {Data.Name}, Age: {Data.Age}");
    }
}

```

This example writes the data from the request to file.bin. Message.Data only contains the payload of the request, not the headers of it. 
```cs
Client.MessageRecieved += Client_MessageRecieved;

private void Client_MessageRecieved(SnooperMessage Message)
{
    Console.WriteLine($"Message Size: {Message.Data.Length}");
    using (FileStream FS = new FileStream("File.bin", FileMode.OpenOrCreate))
        Message.Data.CopyTo(FS);
}
```


## Channels

Snooper Sockets provide a channels system. This allows you to create different channels for different message types. E.g., a channel of Chat messages, and another for Player Events.

### Sending through a channel.

You can specify the message channel when sending data with SnooperSocketClient.Write method. Here, the 'MyData' class variable is being sent to the "Persons" channel. These messages will still show from SnooperSocketClient.MessageRecieved event. However, you can use SnooperMessage.Requesthandled to check if the request has already been handled.

```cs
MyData Data = new MyData() { Age = 21, Name = "David" };
Client.Write(Data, null, "Persons");
```

### Receiving from a channel 

You can subscribe to channel messages using SnooperSocketClient.Channels. Specify the channel through the variable key, then you can subscribe to the MessageReceived event of the returned SnooperChannel object.

Here, SnooperSocketClient.MessageRecieved will still recieve the request if SnooperSocketClient.RaiseHandledRequests is True (it is true by default). However, since the channel's event handler/s already recieved the request, the SnooperMessage.Requesthandled will be true. 

```cs
Client.Channels["Persons"].MessageReceived += OnPerson;

private void OnPerson(SnooperMessage message)
{
    MyData Data = message.ReadObject<MyData>();
    Console.WriteLine($"Name: {Data.Name}, Age: {Data.Age}");
}
```

## Headers

You can supply headers with your messages. These headers can be accesssed from SnooperMessage.Headers. You can supply them when sending a message.

In this example, the message is sent to the Persons channel, with an extra header with a name of 'Status' and a value of 'UpdatePerson'.

```cs
Dictionary<string, string> Headers = new Dictionary<string, string>();
Headers.Add("Status", "UpdatePerson");

MyData Data = new MyData() { Age = 21, Name = "David" };
Client.Write(Data, Headers, "Persons");
```

You can read the headers from SnooperMessage.Headers. Keep in mind however, the headers is also used to transfer the socket information, such as object type and channel. These headers are prefixed with a '$'.

```cs
Client.Channels["Persons"].MessageReceived += OnPerson;

private  void OnPerson(SnooperMessage message)
{
    MyData Data = message.ReadObject<MyData>();
    Console.WriteLine($"Name: {Data.Name}, Age: {Data.Age}");
    if (message.Headers.ContainsKey("Status")) {
        string Mode = message.Headers["Status"];
        if (Mode == "UpdatePerson")
        {
            // Stuff
        } else
        {
            //Different Stuff
        }
    }
}
```

I'll eventually flush out documentation, But for now, if you want to see how to use server-side features such as SnooperSocketClientPools and SnooperPoolChannels, view the demo project, SSocketChatTest. 

