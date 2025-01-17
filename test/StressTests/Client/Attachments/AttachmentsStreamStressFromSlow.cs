﻿using System.Threading.Tasks;
using SlowTests.Client.Attachments;
using Tests.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace StressTests.Client.Attachments
{
    public class AttachmentsStreamStressFromSlow : NoDisposalNoOutputNeeded
    {
        public AttachmentsStreamStressFromSlow(ITestOutputHelper output) : base(output)
        {
        }

        [RavenMultiplatformTheory(RavenTestCategory.Attachments, RavenArchitecture.AllX64)]
        [InlineData(128 * 1024 * 1024)]
        [InlineData(1024 * 1024 * 1024)]
        public void CanGetOneAttachment(int size)
        {
            using (var test = new AttachmentsStreamTests(Output))
            {
                test.CanGetOneAttachment(size);
            }
        }

        [RavenMultiplatformTheory(RavenTestCategory.Attachments, RavenArchitecture.AllX64)]
        [InlineData(128 * 1024 * 1024)]
        public async Task CanGetOneAttachmentAsync(int size)
        {
            using (var test = new AttachmentsStreamTests(Output))
            {
                await test.CanGetOneAttachmentAsync(size);
            }
        }

        [Theory]
        [InlineData(1_000, 32768)]
        [InlineData(10_000, 1)]
        public void CanGetListOfAttachmentsAndSkip(int count, int size)
        {
            using (var test = new AttachmentsStreamTests(Output))
            {
                test.CanGetListOfAttachmentsAndSkip(count, size);
            }
        }

        [Theory]
        [InlineData(1_000, 32768)]
        [InlineData(10_000, 1)]
        public void CanGetListOfAttachmentsAndReadOrdered(int count, int size)
        {
            using (var test = new AttachmentsStreamTests(Output))
            {
                test.CanGetListOfAttachmentsAndReadOrdered(count, size);
            }
        }

        [Theory]
        [InlineData(1_000, 32768)]
        [InlineData(10_000, 1)]
        public async Task CanGetListOfAttachmentsAndReadOrderedAsync(int count, int size)
        {
            using (var test = new AttachmentsStreamTests(Output))
            {
                await test.CanGetListOfAttachmentsAndReadOrderedAsync(count, size);
            }
        }
    }
}
