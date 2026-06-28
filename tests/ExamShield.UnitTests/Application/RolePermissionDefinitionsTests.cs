using ExamShield.Application.RolePermissions;
using ExamShield.Domain.Enums;
using FluentAssertions;

namespace ExamShield.UnitTests.Application;

public sealed class RolePermissionDefinitionsTests
{
    [Fact]
    public void All_IsNotEmpty()
    {
        RolePermissionDefinitions.All.Should().NotBeEmpty();
    }

    [Fact]
    public void All_EachRoleHasUniqueUserRole()
    {
        var roles = RolePermissionDefinitions.All.Select(r => r.Role).ToList();
        roles.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void All_EachDefinitionHasAtLeastOnePermission()
    {
        RolePermissionDefinitions.All.Should().AllSatisfy(
            d => d.Permissions.Should().NotBeEmpty($"Role {d.Role} must have at least one permission"));
    }

    [Fact]
    public void All_NoNullOrEmptyDisplayNames()
    {
        RolePermissionDefinitions.All.Should().AllSatisfy(
            d => d.DisplayName.Should().NotBeNullOrWhiteSpace());
    }

    [Fact]
    public void All_NoNullOrEmptyDescriptions()
    {
        RolePermissionDefinitions.All.Should().AllSatisfy(
            d => d.Description.Should().NotBeNullOrWhiteSpace());
    }

    [Theory]
    [InlineData(UserRole.Operator, "capture.read")]
    [InlineData(UserRole.Operator, "capture.write")]
    [InlineData(UserRole.Auditor, "audit.read")]
    [InlineData(UserRole.Administrator, "users.manage")]
    [InlineData(UserRole.Supervisor, "score.write")]
    public void Role_HasExpectedPermission(UserRole role, string permission)
    {
        var def = RolePermissionDefinitions.All.FirstOrDefault(d => d.Role == role);
        def.Should().NotBeNull($"Role {role} should be defined");
        def!.Permissions.Should().Contain(permission);
    }

    [Theory]
    [InlineData(UserRole.Operator, "audit.read")]
    [InlineData(UserRole.Auditor, "capture.write")]
    public void RestrictedRole_LacksPrivilegedPermission(UserRole role, string permission)
    {
        var def = RolePermissionDefinitions.All.FirstOrDefault(d => d.Role == role);
        def.Should().NotBeNull();
        def!.Permissions.Should().NotContain(permission);
    }
}
