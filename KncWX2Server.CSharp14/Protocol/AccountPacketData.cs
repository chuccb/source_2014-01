namespace KncWX2Server.CSharp14.Protocol;

public sealed class KAccountInfo
{
    public long UserUid { get; set; }
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int AuthLevel { get; set; }
    public bool InternalUser { get; set; }
    public KAccountOption AccountOption { get; set; } = new();
    public KAccountBlockInfo AccountBlockInfo { get; set; } = new();
    public bool IsRecommend { get; set; }
    public bool IsGuestUser { get; set; }
    public string Otp { get; set; } = string.Empty;
    public string RegDate { get; set; } = string.Empty;
    public string LastLogin { get; set; } = string.Empty;
    public int ChannelRandomKey { get; set; }
    public string LogoutDate { get; set; } = string.Empty;

    public bool Serialize(NativePrimitiveSerializer serializer, ProtocolOptions? options = null)
    {
        options ??= ProtocolOptions.Default;
        return new NativeUserClassSerializer(serializer).Put(this, (ser, value) =>
        {
            ser.Put(value.UserUid);
            ser.PutWString(value.Id);
            ser.PutWString(value.Name);
            ser.Put(value.AuthLevel);
            ser.Put(value.InternalUser);
            if (!value.AccountOption.Serialize(ser) || !value.AccountBlockInfo.Serialize(ser)) return false;
            ser.Put(value.IsRecommend);
            ser.Put(value.IsGuestUser);
            ser.PutWString(value.Otp);
            if (options.CashItemList) ser.PutWString(value.RegDate);
            if (options.SecondSecurity) ser.PutWString(value.LastLogin);
            if (options.DllListCheckBeforeLoading) ser.Put(value.ChannelRandomKey);
            if (options.FixedDateEvent) ser.PutWString(value.LogoutDate);
            return true;
        });
    }

    public static bool TryDeserialize(NativePrimitiveSerializer serializer, out KAccountInfo value, ProtocolOptions? options = null)
    {
        options ??= ProtocolOptions.Default;
        value = new();
        return new NativeUserClassSerializer(serializer).TryGet(out value, (ser, existing) =>
        {
            if (!ser.TryGet(out long uid) || !ser.TryGetWString(out var id) || !ser.TryGetWString(out var name) ||
                !ser.TryGet(out int authLevel) || !ser.TryGet(out bool internalUser) ||
                !KAccountOption.TryDeserialize(ser, out var accountOption) ||
                !KAccountBlockInfo.TryDeserialize(ser, out var blockInfo) ||
                !ser.TryGet(out bool recommend) || !ser.TryGet(out bool guest) || !ser.TryGetWString(out var otp))
                return (false, existing);

            existing.UserUid = uid;
            existing.Id = id;
            existing.Name = name;
            existing.AuthLevel = authLevel;
            existing.InternalUser = internalUser;
            existing.AccountOption = accountOption;
            existing.AccountBlockInfo = blockInfo;
            existing.IsRecommend = recommend;
            existing.IsGuestUser = guest;
            existing.Otp = otp;
            if (options.CashItemList && !ser.TryGetWString(out existing.RegDate)) return (false, existing);
            if (options.SecondSecurity && !ser.TryGetWString(out existing.LastLogin)) return (false, existing);
            if (options.DllListCheckBeforeLoading && !ser.TryGet(out existing.ChannelRandomKey)) return (false, existing);
            if (options.FixedDateEvent && !ser.TryGetWString(out existing.LogoutDate)) return (false, existing);
            return (true, existing);
        });
    }
}

public sealed class KUserAuthenticateReq
{
    public bool DebugAuth { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Hwid { get; set; } = string.Empty;
    public string MachineId { get; set; } = string.Empty;
    public int ChannelRandomKey { get; set; }
    public byte[] ServerSn { get; set; } = new byte[12];
    public string ClientTime { get; set; } = string.Empty;
    public int ChannelingCode { get; set; }
    public bool ManualLogin { get; set; }
    public string SocketId { get; set; } = string.Empty;
    public bool SteamClient { get; set; }
    public bool AeriaClient { get; set; }

    public bool Serialize(NativePrimitiveSerializer serializer, ProtocolOptions? options = null)
    {
        options ??= ProtocolOptions.Default;
        if (ServerSn.Length != 12) throw new ArgumentException("Native SERVER_SN is exactly 12 bytes.", nameof(ServerSn));

        return new NativeUserClassSerializer(serializer).Put(this, (ser, value) =>
        {
            ser.Put(value.DebugAuth);
            ser.PutWString(value.UserId);
            ser.PutWString(value.Password);
            if (options.OtpAuth) ser.PutWString(value.Hwid);
            if (options.MachineIdDuplicateCheck) ser.Put(value.MachineId);
            if (options.DllListCheckBeforeLoading) ser.Put(value.ChannelRandomKey);
            if (options.SerialNumberAvailabilityCheckInGameServer)
                foreach (var b in value.ServerSn) ser.Put(b);
            if (options.CheckMachineLocalTime) ser.PutWString(value.ClientTime);
            ser.Put(value.ChannelingCode);
            if (options.CogOtpVerify) ser.Put(value.ManualLogin);
            if (options.CountryTh) ser.PutWString(value.SocketId);
            if (options.Steam) ser.Put(value.SteamClient);
            if (options.ChannelingAeria) ser.Put(value.AeriaClient);
            return true;
        });
    }

    public static bool TryDeserialize(NativePrimitiveSerializer serializer, out KUserAuthenticateReq value, ProtocolOptions? options = null)
    {
        options ??= ProtocolOptions.Default;
        value = new();
        return new NativeUserClassSerializer(serializer).TryGet(out value, (ser, existing) =>
        {
            if (!ser.TryGet(out bool debug) || !ser.TryGetWString(out var userId) || !ser.TryGetWString(out var password))
                return (false, existing);
            existing.DebugAuth = debug;
            existing.UserId = userId;
            existing.Password = password;

            if (options.OtpAuth && !ser.TryGetWString(out existing.Hwid)) return (false, existing);
            if (options.MachineIdDuplicateCheck && !ser.TryGetString(out existing.MachineId)) return (false, existing);
            if (options.DllListCheckBeforeLoading && !ser.TryGet(out existing.ChannelRandomKey)) return (false, existing);
            if (options.SerialNumberAvailabilityCheckInGameServer)
            {
                existing.ServerSn = new byte[12];
                for (var i = 0; i < existing.ServerSn.Length; i++)
                    if (!ser.TryGet(out existing.ServerSn[i])) return (false, existing);
            }
            if (options.CheckMachineLocalTime && !ser.TryGetWString(out existing.ClientTime)) return (false, existing);
            if (!ser.TryGet(out existing.ChannelingCode)) return (false, existing);
            if (options.CogOtpVerify && !ser.TryGet(out existing.ManualLogin)) return (false, existing);
            if (options.CountryTh && !ser.TryGetWString(out existing.SocketId)) return (false, existing);
            if (options.Steam && !ser.TryGet(out existing.SteamClient)) return (false, existing);
            if (options.ChannelingAeria && !ser.TryGet(out existing.AeriaClient)) return (false, existing);
            return (true, existing);
        });
    }
}
