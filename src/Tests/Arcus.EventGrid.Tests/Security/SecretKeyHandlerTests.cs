using System;
using System.Collections.Generic;
using System.Text;
using Arcus.EventGrid.Security;
using Xunit;

namespace Arcus.EventGrid.Tests.Security
{
    public class SecretKeyHandlerTests
    {
        [Theory]
        [InlineData(true, null, false)]
        [InlineData(true, "", false)]
        [InlineData(true, "0", true)]
        [InlineData(false, null, true)]
        public void SecretKeyNameValidation(bool setName, string secretKeyName, bool shouldSucceed)
        {
            SecretKeyHandler.SecretKeyRetriever = () => "key";
            var keyHandler = setName ? new SecretKeyHandler(secretKeyName) : new SecretKeyHandler();
            if (!shouldSucceed)
            {
                Assert.Throws<ArgumentException>(() =>
                {
                    keyHandler.ValidateConfiguration();
                });
            }
            else
            {
                keyHandler.ValidateConfiguration();
            }
        }

        [Theory]
        [InlineData(true, true, "key", true)]
        [InlineData(false, true, "key", true)]
        [InlineData(true, false, "key", true)]
        [InlineData(false, false, null, false)]
        [InlineData(true, true, null, false)]
        [InlineData(false, true, null, false)]
        [InlineData(true, false, null, false)]
        public void SecretKeyValueValidation(bool setRetriever, bool setValue, string secretKeyValue, bool shouldSucceed)
        {
            if (setRetriever)
            {
                SecretKeyHandler.SecretKeyRetriever = () => secretKeyValue;
            }
            var keyHandler = setValue ?
                new SecretKeyHandler("x-api-key", secretKeyValue) :
                new SecretKeyHandler();
            if (!shouldSucceed)
            {
                Assert.Throws<ArgumentException>(() =>
                {
                    keyHandler.ValidateConfiguration();
                });
            }
            else
            {
                keyHandler.ValidateConfiguration();
            }
        }
    }
}