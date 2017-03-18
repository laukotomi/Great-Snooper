namespace GreatSnooper.IRC
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    public class WormNetCharTable
    {
        #region Singleton
        private static readonly Lazy<WormNetCharTable> lazy =
            new Lazy<WormNetCharTable>(() => new WormNetCharTable());

        public static WormNetCharTable Instance
        {
            get
            {
                return lazy.Value;
            }
        }

        private WormNetCharTable()
        {

        }
        #endregion

        // A table to decode the messages sent from the WormNet servers. The encoding table will be generated from this.
        private char[] _decode =
        {
            ////x0      x1      x2      x3      x4      x5      x6      x7      x8      x9      xA      xB      xC      xD      xE      xF
            '\x00', '\x01', '\x02', '\x03', '\x04', '\x05', '\x06', '\x07', '\x08', '\x09', '\x0A', '\x0B', '\x0C', '\x0D', '\x0E', '\x0F', // 0x
            '\x10', '\x11', '\x12', '\x13', '\x14', '\x15', '\x16', '\x17', '\x18', '\x19', '\x1A', '\x1B', '\x1C', '\x1D', '\x1E', '\x1F', // 1x
            ' ',    '!',    '"',    '#',    '$',    '%',    '&',    '\'',   '(',    ')',    '*',    '+',    ',',    '-',    '.',    '/',    // 2x
            '0',    '1',    '2',    '3',    '4',    '5',    '6',    '7',    '8',    '9',    ':',    ';',    '<',    '=',    '>',    '?',    // 3x
            '@',    'A',    'B',    'C',    'D',    'E',    'F',    'G',    'H',    'I',    'J',    'K',    'L',    'M',    'N',    'O',    // 4x
            'P',    'Q',    'R',    'S',    'T',    'U',    'V',    'W',    'X',    'Y',    'Z',    '[',    '\\',   ']',    '^',    '_',    // 5x
            '`',    'a',    'b',    'c',    'd',    'e',    'f',    'g',    'h',    'i',    'j',    'k',    'l',    'm',    'n',    'o',    // 6x
            'p',    'q',    'r',    's',    't',    'u',    'v',    'w',    'x',    'y',    'z',    '{',    '|',    '}',    '~',    '\0',   // 7x
            'Б',    'Г',    'Д',    'Ж',    'З',    'И',    'Й',    'К',    'Л',    'П',    'У',    'Ф',    'Ц',    'Ч',    'Ш',    'Щ',    // 8x
            'Ъ',    'Ы',    'Ь',    'Э',    'Ю',    '\0',   'Я',    'б',    'в',    'г',    'д',    'ж',    'з',    'и',    'й',    'Ÿ',    // 9x
            '\b',   '¡',    'к',    '£',    '\0',   'л',    'м',    'н',    'п',    'т',    'ф',    'ц',    'ч',    'ш',    'щ',    'ъ',    // Ax
            'ы',    'ь',    'э',    'ю',    'я',    'Ő',    'ő',    'Ű',    'ű',    '\0',   '\0',   '\0',   '\0',   '\0',   '\0',   '¿',    // Bx
            'À',    'Á',    'Â',    'Ã',    'Ä',    'Å',    'Æ',    'Ç',    'È',    'É',    'Ê',    'Ë',    'Ì',    'Í',    'Î',    'Ï',    // Cx
            'Ð',    'Ñ',    'Ò',    'Ó',    'Ô',    'Õ',    'Ö',    '×',    'Ø',    'Ù',    'Ú',    'Û',    'Ü',    'Ý',    'Þ',    'ß',    // Dx
            'à',    'á',    'â',    'ã',    'ä',    'å',    'æ',    'ç',    'è',    'é',    'ê',    'ë',    'ì',    'í',    'î',    'ï',    // Ex
            'ð',    'ñ',    'ò',    'ó',    'ô',    'õ',    'ö',    '÷',    'ø',    'ù',    'ú',    'û',    'ü',    'ý',    'þ',    'ÿ'     // Fx
        };
        private char[] _decodeGame = new char[256];

        // A table to encode messages to send them to the WormNet (will be generated in the constructor)
        private Dictionary<char, byte> _encode = new Dictionary<char, byte>();
        private Dictionary<char, byte> _encodeGame = new Dictionary<char, byte>();

        // This method ensures that the initialization will be made from the appropriate thread
        private bool _isInitialized;
        public void Initialize()
        {
            if (_isInitialized)
            {
                return;
            }
            _isInitialized = true;

            for (int i = 0; i < _decode.Length; i++)
            {
                // Generate the encode dictionary
                if (!_encode.ContainsKey(_decode[i]))
                {
                    _encode.Add(_decode[i], System.Convert.ToByte(i));    // We mix up the key - value pairs
                }

                // Generate the decode array for games
                _decodeGame[i] = _decode[i];
            }

            AddCyrillSupport(_encode);

            // <Deadcode> the five characters "&'<>\ are mapped to %10%11%12%13%14%15
            _decodeGame[0x10] = '"';
            _decodeGame[0x11] = '&';
            _decodeGame[0x12] = '\'';
            _decodeGame[0x13] = '<';
            _decodeGame[0x14] = '>';
            _decodeGame[0x15] = '\\';

            // Generate the encode dictionary for games
            for (int i = 0; i < _decodeGame.Length; i++)
            {
                // Generate the encode dictionary for games (WormNet GameList.asp, etc. can't use ; character!)
                if (_decodeGame[i] != ';' && !_encodeGame.ContainsKey(_decodeGame[i]))
                {
                    _encodeGame.Add(_decodeGame[i], System.Convert.ToByte(i));    // We mix up the key - value pairs
                }
            }

            AddCyrillSupport(_encodeGame);
        }

        private void AddCyrillSupport(Dictionary<char, byte> obj)
        {
            // Add missing cyrill letters to the Encode dictionary
            obj.Add('А', System.Convert.ToByte(0x41));
            obj.Add('В', System.Convert.ToByte(0x42));
            obj.Add('С', System.Convert.ToByte(0x43));
            obj.Add('Е', System.Convert.ToByte(0x45));
            obj.Add('Н', System.Convert.ToByte(0x48));
            obj.Add('М', System.Convert.ToByte(0x4D));
            obj.Add('О', System.Convert.ToByte(0x4F));
            obj.Add('Р', System.Convert.ToByte(0x50));
            obj.Add('Т', System.Convert.ToByte(0x54));
            obj.Add('Х', System.Convert.ToByte(0x58));
            obj.Add('а', System.Convert.ToByte(0x61));
            obj.Add('с', System.Convert.ToByte(0x63));
            obj.Add('е', System.Convert.ToByte(0x65));
            obj.Add('о', System.Convert.ToByte(0x6F));
            obj.Add('р', System.Convert.ToByte(0x70));
            obj.Add('х', System.Convert.ToByte(0x78));
            obj.Add('у', System.Convert.ToByte(0x79));
            obj.Add('Ё', System.Convert.ToByte(0xCB));
            obj.Add('ё', System.Convert.ToByte(0xEB));
        }

        public string Decode(byte[] bytes, int length)
        {
            return Decode(bytes, length, _decode);
        }

        public string DecodeGame(byte[] bytes, int length)
        {
            return Decode(bytes, length, _decodeGame);
        }

        private string Decode(byte[] bytes, int length, char[] chars)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < length && i < bytes.Length; i++)
            {
                sb.Append(chars[bytes[i]]);
            }
            return sb.ToString();
        }

        public byte EncodeChar(char ch)
        {
            return _encode[ch];
        }

        // Remove non-wormnet characters from a string
        public string RemoveNonWormNetChars(string str)
        {
            return RemoveIllegalChars(str, _encode);
        }

        public string RemoveNonGameChars(string str)
        {
            return RemoveIllegalChars(str, _encodeGame);
        }

        private string RemoveIllegalChars(string str, Dictionary<char, byte> map)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < str.Length; i++)
            {
                char ch = str[i];
                if (map.ContainsKey(ch))
                {
                    sb.Append(ch);
                }
            }
            return sb.ToString();
        }

        internal string EncodeGameUrl(string str)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < str.Length; i++)
            {
                char ch = str[i];
                if (ch == '"' || ch == '&' || ch == '\'' || ch == '<' || ch == '>' || ch == '\\')
                {
                    sb.Append("%" + _encodeGame[ch].ToString("X"));
                }
                else if (ch == '#' || ch == '+' || ch == '%')
                {
                    sb.Append("%" + _encodeGame[ch].ToString("X"));
                    i += 2;
                }
                else if (ch == ' ')
                {
                    sb.Append("%A0");
                }
                else if (_encodeGame[ch] >= 0x80)
                {
                    sb.Append("%" + _encodeGame[ch].ToString("X"));
                }
            }
            return sb.ToString();
        }

        internal int GetBytes(string message, int charIndex, int charCount, byte[] bytes, int byteIndex)
        {
            int i = 0;
            for (; i < charCount && charIndex < message.Length && byteIndex < bytes.Length; i++, charIndex++, byteIndex++)
            {
                bytes[byteIndex] = _encode[message[charIndex]];
            }
            return i;
        }

        public char DecodeByte(byte b)
        {
            return _decode[b];
        }
    }
}