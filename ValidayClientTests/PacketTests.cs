using System;
using System.Text;
using Xunit;
using ValidayClient.Network;
using ValidayClient.Network.Commands;
using ValidayClient.Network.Commands.Interfaces;

namespace ValidayClientTests
{
    public class PacketTests
    {
        // ── PacketWriter ────────────────────────────────────────────────────────

        [Fact]
        public void PacketWriter_Build_StartsWithCommandId()
        {
            byte[] data = new PacketWriter(42).Build();

            Assert.Equal((ushort)42, BitConverter.ToUInt16(data, 0));
        }

        [Fact]
        public void PacketWriter_Build_EmptyPayload_TwoBytes()
        {
            byte[] data = new PacketWriter(1).Build();

            Assert.Equal(2, data.Length);
        }

        [Fact]
        public void PacketWriter_BuildFramed_HasLengthPrefix()
        {
            byte[] data = new PacketWriter(1).WriteInt(99).BuildFramed();

            ushort bodyLength = BitConverter.ToUInt16(data, 0);
            Assert.Equal(data.Length - 2, bodyLength);
        }

        [Fact]
        public void PacketWriter_WriteRead_Byte_RoundTrip()
        {
            byte[] data = new PacketWriter(1).WriteByte(200).Build();
            byte result = new PacketReader(data).SkipCommandId().ReadByte();

            Assert.Equal(200, result);
        }

        [Fact]
        public void PacketWriter_WriteRead_Bool_True_RoundTrip()
        {
            byte[] data = new PacketWriter(1).WriteBool(true).Build();
            bool result = new PacketReader(data).SkipCommandId().ReadBool();

            Assert.True(result);
        }

        [Fact]
        public void PacketWriter_WriteRead_Bool_False_RoundTrip()
        {
            byte[] data = new PacketWriter(1).WriteBool(false).Build();
            bool result = new PacketReader(data).SkipCommandId().ReadBool();

            Assert.False(result);
        }

        [Fact]
        public void PacketWriter_WriteRead_Short_RoundTrip()
        {
            byte[] data = new PacketWriter(1).WriteShort(-1234).Build();
            short result = new PacketReader(data).SkipCommandId().ReadShort();

            Assert.Equal((short)-1234, result);
        }

        [Fact]
        public void PacketWriter_WriteRead_Ushort_RoundTrip()
        {
            byte[] data = new PacketWriter(1).WriteUshort(60000).Build();
            ushort result = new PacketReader(data).SkipCommandId().ReadUshort();

            Assert.Equal((ushort)60000, result);
        }

        [Fact]
        public void PacketWriter_WriteRead_Int_RoundTrip()
        {
            byte[] data = new PacketWriter(1).WriteInt(-99999).Build();
            int result = new PacketReader(data).SkipCommandId().ReadInt();

            Assert.Equal(-99999, result);
        }

        [Fact]
        public void PacketWriter_WriteRead_Uint_RoundTrip()
        {
            byte[] data = new PacketWriter(1).WriteUint(3000000000u).Build();
            uint result = new PacketReader(data).SkipCommandId().ReadUint();

            Assert.Equal(3000000000u, result);
        }

        [Fact]
        public void PacketWriter_WriteRead_Long_RoundTrip()
        {
            byte[] data = new PacketWriter(1).WriteLong(long.MinValue).Build();
            long result = new PacketReader(data).SkipCommandId().ReadLong();

            Assert.Equal(long.MinValue, result);
        }

        [Fact]
        public void PacketWriter_WriteRead_Float_RoundTrip()
        {
            byte[] data = new PacketWriter(1).WriteFloat(3.14f).Build();
            float result = new PacketReader(data).SkipCommandId().ReadFloat();

            Assert.Equal(3.14f, result);
        }

        [Fact]
        public void PacketWriter_WriteRead_Double_RoundTrip()
        {
            byte[] data = new PacketWriter(1).WriteDouble(Math.PI).Build();
            double result = new PacketReader(data).SkipCommandId().ReadDouble();

            Assert.Equal(Math.PI, result);
        }

        [Fact]
        public void PacketWriter_WriteRead_String_RoundTrip()
        {
            byte[] data = new PacketWriter(1).WriteString("Hello, World!").Build();
            string result = new PacketReader(data).SkipCommandId().ReadString();

            Assert.Equal("Hello, World!", result);
        }

        [Fact]
        public void PacketWriter_WriteRead_String_Empty_RoundTrip()
        {
            byte[] data = new PacketWriter(1).WriteString(string.Empty).Build();
            string result = new PacketReader(data).SkipCommandId().ReadString();

            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void PacketWriter_WriteRead_String_Null_TreatedAsEmpty()
        {
            byte[] data = new PacketWriter(1).WriteString(null).Build();
            string result = new PacketReader(data).SkipCommandId().ReadString();

            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void PacketWriter_WriteRead_String_Unicode_RoundTrip()
        {
            const string text = "Привет, мир! 🌍";
            byte[] data = new PacketWriter(1).WriteString(text).Build();
            string result = new PacketReader(data).SkipCommandId().ReadString();

            Assert.Equal(text, result);
        }

        [Fact]
        public void PacketWriter_WriteRead_Bytes_RoundTrip()
        {
            byte[] payload = { 10, 20, 30, 40 };
            byte[] data = new PacketWriter(1).WriteBytes(payload).Build();
            byte[] result = new PacketReader(data).SkipCommandId().ReadBytes(4);

            Assert.Equal(payload, result);
        }

        [Fact]
        public void PacketWriter_WriteRead_BytesWithLength_RoundTrip()
        {
            byte[] payload = { 5, 10, 15 };
            byte[] data = new PacketWriter(1).WriteBytes(payload, lengthPrefixed: true).Build();
            byte[] result = new PacketReader(data).SkipCommandId().ReadBytesWithLength();

            Assert.Equal(payload, result);
        }

        [Fact]
        public void PacketWriter_MultipleFields_CorrectOrder()
        {
            byte[] data = new PacketWriter(7)
                .WriteInt(42)
                .WriteString("hi")
                .WriteBool(true)
                .Build();

            var reader = new PacketReader(data).SkipCommandId();

            Assert.Equal(42, reader.ReadInt());
            Assert.Equal("hi", reader.ReadString());
            Assert.True(reader.ReadBool());
        }

        // ── PacketReader ────────────────────────────────────────────────────────

        [Fact]
        public void PacketReader_SkipCommandId_AdvancesBy2()
        {
            byte[] data = { 0x01, 0x00, 0xFF };
            var reader = new PacketReader(data);
            reader.SkipCommandId();

            Assert.Equal(2, reader.Position);
        }

        [Fact]
        public void PacketReader_Skip_AdvancesPosition()
        {
            byte[] data = new PacketWriter(1).WriteInt(0).WriteInt(99).Build();
            var reader = new PacketReader(data).SkipCommandId().Skip(4);

            Assert.Equal(99, reader.ReadInt());
        }

        [Fact]
        public void PacketReader_Remaining_DecreasesOnRead()
        {
            byte[] data = new PacketWriter(1).WriteInt(0).Build();
            var reader = new PacketReader(data).SkipCommandId();
            int before = reader.Remaining;
            reader.ReadInt();

            Assert.Equal(before - 4, reader.Remaining);
        }

        [Fact]
        public void PacketReader_Offset_Constructor_StartsAtOffset()
        {
            byte[] data = new PacketWriter(1).WriteByte(10).WriteByte(20).Build();
            var reader = new PacketReader(data, offset: 3);

            Assert.Equal(20, reader.ReadByte());
        }

        // ── ServerCommandBase ───────────────────────────────────────────────────

        [Fact]
        public void ServerCommandBase_GetRawData_ContainsCommandId()
        {
            var cmd = new SampleServerCommand(commandId: 5) { Value = 42 };
            byte[] data = cmd.GetRawData();

            Assert.Equal((ushort)5, BitConverter.ToUInt16(data, 0));
        }

        [Fact]
        public void ServerCommandBase_GetRawData_ContainsPayload()
        {
            var cmd = new SampleServerCommand(commandId: 1) { Value = 99 };
            byte[] data = cmd.GetRawData();
            int value = BitConverter.ToInt32(data, 2);

            Assert.Equal(99, value);
        }

        // ── ClientCommandBase ───────────────────────────────────────────────────

        [Fact]
        public void ClientCommandBase_Execute_DeserializesAndHandles()
        {
            byte[] data = new PacketWriter(3).WriteString("test").WriteInt(7).Build();
            var cmd = new SampleClientCommand();

            cmd.Execute(data);

            Assert.Equal("test", cmd.LastMessage);
            Assert.Equal(7, cmd.LastValue);
        }

        [Fact]
        public void ClientCommandBase_Execute_SkipsCommandId()
        {
            byte[] data = new PacketWriter(99).WriteFloat(1.5f).Build();
            var cmd = new FloatClientCommand();

            cmd.Execute(data);

            Assert.Equal(1.5f, cmd.LastValue);
        }

        // ── End-to-end: full packet lifecycle ───────────────────────────────────

        [Fact]
        public void FullLifecycle_ServerCommandBase_To_ClientCommandBase()
        {
            var outgoing = new ChatServerCommand { Message = "Hello", PlayerId = 42 };
            byte[] wire = outgoing.GetRawData();

            var incoming = new ChatClientCommand();
            incoming.Execute(wire);

            Assert.Equal("Hello", incoming.LastMessage);
            Assert.Equal(42, incoming.LastPlayerId);
        }

        // ── Test helpers ────────────────────────────────────────────────────────

        class SampleServerCommand : ServerCommandBase
        {
            public int Value { get; set; }
            public SampleServerCommand(ushort commandId) : base(commandId) { }
            protected override void Write(PacketWriter writer) => writer.WriteInt(Value);
        }

        record SamplePayload(string Message, int Value);

        class SampleClientCommand : ClientCommandBase<SamplePayload>
        {
            public string LastMessage { get; private set; } = string.Empty;
            public int LastValue { get; private set; }

            protected override SamplePayload Read(PacketReader reader)
                => new SamplePayload(reader.ReadString(), reader.ReadInt());

            protected override void Handle(SamplePayload payload)
            {
                LastMessage = payload.Message;
                LastValue = payload.Value;
            }
        }

        class FloatClientCommand : ClientCommandBase<float>
        {
            public float LastValue { get; private set; }
            protected override float Read(PacketReader reader) => reader.ReadFloat();
            protected override void Handle(float payload) => LastValue = payload;
        }

        record ChatPayload(string Message, int PlayerId);

        class ChatServerCommand : ServerCommandBase
        {
            public string Message { get; set; } = string.Empty;
            public int PlayerId { get; set; }
            public ChatServerCommand() : base(commandId: 10) { }
            protected override void Write(PacketWriter writer)
                => writer.WriteString(Message).WriteInt(PlayerId);
        }

        class ChatClientCommand : ClientCommandBase<ChatPayload>
        {
            public string LastMessage { get; private set; } = string.Empty;
            public int LastPlayerId { get; private set; }

            protected override ChatPayload Read(PacketReader reader)
                => new ChatPayload(reader.ReadString(), reader.ReadInt());

            protected override void Handle(ChatPayload payload)
            {
                LastMessage = payload.Message;
                LastPlayerId = payload.PlayerId;
            }
        }
    }
}
