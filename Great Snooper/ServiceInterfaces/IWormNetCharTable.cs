
namespace GreatSnooper.ServiceInterfaces
{
    interface IWormNetCharTable
    {
        char[] Decode { get; }
        char[] DecodeGame { get; }
        byte GetByteForChar(char c);
        string Encode(string input);
        string EncodeGame(string input);
        string EncodeGameUrl(string input);
    }
}
