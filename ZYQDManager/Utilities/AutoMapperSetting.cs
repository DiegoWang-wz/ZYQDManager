using AutoMapper;

namespace ZYQDManager.Utilities;

public class AutoMapperSetting : Profile
{
    public AutoMapperSetting()
    {
        CreateMap<byte, bool>().ConvertUsing(src => src == 1);
        CreateMap<bool, byte>().ConvertUsing(src => src ? (byte)1 : (byte)0);
    }
}
