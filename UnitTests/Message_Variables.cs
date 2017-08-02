﻿using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using MailMergeLib;
using MailMergeLib.Templates;

namespace UnitTests
{
    [TestFixture]
    class Message_Variables
    {
        [Test]
        public void MissingVariableAndAttachmentsExceptions()
        {
            // build message with a total of 9 placeholders which will be missing
            var mmm = new MailMergeMessage("Missing in subject {subject}", "Missing in plain text {plain}",
                "<html><head></head><body>{html}{:template(Missing)}</body></html>");
            mmm.MailMergeAddresses.Add(new MailMergeAddress(MailAddressType.From, "{from.address}"));
            mmm.MailMergeAddresses.Add(new MailMergeAddress(MailAddressType.To, "{to.address}"));
            mmm.AddExternalInlineAttachment(new FileAttachment("{inlineAtt.filename}.jpg", string.Empty));
            mmm.FileAttachments.Add(new FileAttachment("{fileAtt.filename}.xml", "{fileAtt.displayname}"));

            // **************** Part 1:
            mmm.Config.IgnoreMissingInlineAttachments = false;
            mmm.Config.IgnoreMissingFileAttachments = false;
            // ************************

            try
            {
                mmm.GetMimeMessage(default(object));
                Assert.Fail("Expected exceptions not thrown.");
            }
            catch (MailMergeMessage.MailMergeMessageException exceptions)
            {
                Console.WriteLine($"Aggregate {nameof(MailMergeMessage.MailMergeMessageException)} thrown.");
                Console.WriteLine();
                /* Expected exceptions:
                 * 1) 9 missing variables for {placeholders} and {:templates(...)}
                 * 2) No recipients
                 * 3) No FROM address
                 * 4) Missing file attachment {fileAtt.filename}.xml
                 * 5) Missing inline attachment {inlineAtt.filename}.jpg
                 */
                Assert.AreEqual(5, exceptions.InnerExceptions.Count);

                foreach (var ex in exceptions.InnerExceptions.Where(ex => !(ex is MailMergeMessage
                    .AttachmentException)))
                {
                    if (ex is MailMergeMessage.VariableException)
                    {
                        Assert.AreEqual(9, (ex as MailMergeMessage.VariableException).MissingVariable.Count);
                        Console.WriteLine($"{nameof(MailMergeMessage.VariableException)} thrown successfully:");
                        Console.WriteLine("Missing variables: " +
                                          string.Join(", ",
                                              (ex as MailMergeMessage.VariableException).MissingVariable));
                        Console.WriteLine();
                    }
                    if (ex is MailMergeMessage.AddressException)
                    {
                        Console.WriteLine($"{nameof(MailMergeMessage.AddressException)} thrown successfully:");
                        Console.WriteLine((ex as MailMergeMessage.AddressException).Message);
                        Console.WriteLine();
                        Assert.That((ex as MailMergeMessage.AddressException).Message == "No recipients." ||
                                    (ex as MailMergeMessage.AddressException).Message == "No from address.");
                    }
                }

                // one exception for a missing file attachment, one for a missing inline attachment
                var attExceptions = exceptions.InnerExceptions.Where(ex => ex is MailMergeMessage.AttachmentException)
                    .ToList();
                Assert.AreEqual(2, attExceptions.Count);
                foreach (var ex in attExceptions)
                {
                    Console.WriteLine($"{nameof(MailMergeMessage.AttachmentException)} thrown successfully:");
                    Console.WriteLine("Missing files: " +
                                      string.Join(", ", (ex as MailMergeMessage.AttachmentException).BadAttachment));
                    Console.WriteLine();
                    Assert.AreEqual(1, (ex as MailMergeMessage.AttachmentException).BadAttachment.Count);
                }
            }

            // **************** Part 2:
            mmm.Config.IgnoreMissingInlineAttachments = true;
            mmm.Config.IgnoreMissingFileAttachments = true;
            // ************************

            try
            {
                mmm.GetMimeMessage(default(object));
                Assert.Fail("Expected exceptions not thrown.");
            }
            catch (MailMergeMessage.MailMergeMessageException exceptions)
            {
                /* Expected exceptions:
                 * 1) 9 missing variables for {placeholders} and {:templates(...)}
                 * 2) No recipients
                 * 3) No FROM address
                 */
                Assert.AreEqual(3, exceptions.InnerExceptions.Count);
                Assert.IsFalse(exceptions.InnerExceptions.Any(e => e is MailMergeMessage.AttachmentException));

                Console.WriteLine("Exceptions for missing attachment files suppressed.");
            }
        }
    }
}