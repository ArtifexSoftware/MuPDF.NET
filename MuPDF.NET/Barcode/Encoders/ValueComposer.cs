namespace BarcodeWriter.Core
{
    /// <summary>
    /// Helps to create barcode values for vCard, e-mail, and SMS messages that should be correctly handled by mobile barcode readers.
    /// </summary>
    public static class ValueComposer
    {
        /// <summary>
        /// Creates value that's correctly handled by barcode readers as SMS message. 
        /// </summary>
        /// <param name="phoneNumber">Phone number.</param>
        /// <param name="messageText">Message text.</param>
        /// <returns>String that contains composed SMS message.</returns>
        public static string ComposeSMS(string phoneNumber, string messageText)
        {
            return $"smsto:{phoneNumber}:{messageText}";
        }

        /// <summary>
        /// Creates value that's correctly handled by barcode readers as e-mail message.
        /// </summary>
        /// <param name="emailAddress">E-mail address.</param>
        /// <param name="messageSubject">Message subject.</param>
        /// <param name="messageBody">Message body.</param>
        /// <returns>String that contains composed e-mail message.</returns>
        public static string ComposeEmail(string emailAddress, string messageSubject, string messageBody)
        {
            return $@"MATMSG:TO:{emailAddress};SUB:{messageSubject}BODY:{messageBody};;";
        }

        /// <summary>
        /// Creates value that's correctly handled by barcode readers as vCard message. 
        /// </summary>
        /// <param name="firstName">First name.</param>
        /// <param name="lastName">Last name.</param>
        /// <param name="phone">Phone number</param>
        /// <param name="fax">Fax number</param>
        /// <param name="email">E-mail address.</param>
        /// <param name="company">Company name</param>
        /// <param name="job">Job.</param>
        /// <param name="street">Street.</param>
        /// <param name="city">City.</param>
        /// <param name="state">State.</param>
        /// <param name="zipCode">ZIP (postal) code.</param>
        /// <param name="country">Country.</param>
        /// <returns></returns>
        public static string ComposeVCard(string firstName, string lastName, string phone, string fax, string email, string company, 
            string job, string street, string city, string state, string zipCode, string country)
        {
            return $@"BEGIN:VCARD
VERSION:2.1
N:{lastName};{firstName};;
FN:{firstName} {lastName}
ORG:{company}
TITLE:{job}
TEL;WORK;VOICE:{phone}
TEL;FAX;VOICE:{fax}
ADR;WORK;PREF:;;{street};{city};{state};{zipCode};{country}
LABEL;WORK;PREF;ENCODING=QUOTED-PRINTABLE;CHARSET=UTF-8
EMAIL:{email}
END:VCARD
";
        }
    }
}