namespace GreatSnooper.Validators
{
    public static class Validator
    {
        private static ClanValidator _clanValidator;
        private static GSVersionValidator _gsVersionValidator;
        private static NickNameValidator _nicknameValidator;
        private static NotEmptyValidator _notEmptyValidator;
        private static WormNetTextValidator _wormNetTextValidator;
        private static NumberValidator _numberValidator;

        public static ClanValidator ClanValidator
        {
            get
            {
                if (_clanValidator == null)
                {
                    _clanValidator = new ClanValidator();
                }
                return _clanValidator;
            }
        }

        public static GSVersionValidator GSVersionValidator
        {
            get
            {
                if (_gsVersionValidator == null)
                {
                    _gsVersionValidator = new GSVersionValidator();
                }
                return _gsVersionValidator;
            }
        }

        public static NickNameValidator NickNameValidator
        {
            get
            {
                if (_nicknameValidator == null)
                {
                    _nicknameValidator = new NickNameValidator();
                }
                return _nicknameValidator;
            }
        }

        public static NotEmptyValidator NotEmptyValidator
        {
            get
            {
                if (_notEmptyValidator == null)
                {
                    _notEmptyValidator = new NotEmptyValidator();
                }
                return _notEmptyValidator;
            }
        }

        public static WormNetTextValidator WormNetTextValidator
        {
            get
            {
                if (_wormNetTextValidator == null)
                {
                    _wormNetTextValidator = new WormNetTextValidator();
                }
                return _wormNetTextValidator;
            }
        }

        public static NumberValidator NumberValidator
        {
            get
            {
                if (_numberValidator == null)
                {
                    _numberValidator = new NumberValidator();
                }
                return _numberValidator;
            }
        }
    }
}