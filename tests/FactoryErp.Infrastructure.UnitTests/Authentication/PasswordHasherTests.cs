using FactoryErp.Infrastructure.Authentication;
using FluentAssertions;

namespace FactoryErp.Infrastructure.UnitTests.Authentication;

public sealed class PasswordHasherTests
{
    private readonly PasswordHasher _hasher = new();

    [Fact]
    public void Hash_is_not_plaintext_and_same_password_verifies()
    {
        const string password = "correct-horse-battery-staple";

        var hash = _hasher.Hash(password);

        hash.Should().NotBe(password);
        hash.Should().StartWith("v1$120000$");
        _hasher.Verify(password, hash).Should().BeTrue();
    }

    [Fact]
    public void Same_password_gets_a_new_salt()
    {
        var first = _hasher.Hash("same-password");
        var second = _hasher.Hash("same-password");

        second.Should().NotBe(first);
        _hasher.Verify("same-password", first).Should().BeTrue();
        _hasher.Verify("same-password", second).Should().BeTrue();
    }

    [Fact]
    public void Wrong_password_and_malformed_hash_fail_closed()
    {
        var hash = _hasher.Hash("correct-password");

        _hasher.Verify("wrong-password", hash).Should().BeFalse();
        _hasher.Verify("correct-password", "not-a-valid-hash").Should().BeFalse();
        _hasher.Verify("", hash).Should().BeFalse();
    }
}
