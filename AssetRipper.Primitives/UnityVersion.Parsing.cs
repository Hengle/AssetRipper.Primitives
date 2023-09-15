﻿using AssetRipper.Primitives.Extensions;
using System.Text.RegularExpressions;

namespace AssetRipper.Primitives;

public readonly partial struct UnityVersion
{
	private static readonly Regex majorMinorRegex = new Regex(@"^([0-9]+)\.([0-9]+)$", RegexOptions.Compiled);
	private static readonly Regex majorMinorBuildRegex = new Regex(@"^([0-9]+)\.([0-9]+)\.([0-9]+)$", RegexOptions.Compiled);
	private static readonly Regex normalRegex = new Regex(@"^([0-9]+)\.([0-9]+)\.([0-9]+)\.?([abcfpx])([0-9]+)((?:.|[\r\n])+)?$", RegexOptions.Compiled);
	private static readonly Regex chinaRegex = new Regex(@"^([0-9]+)\.([0-9]+)\.([0-9]+)\.?f1c([0-9]+)((?:.|[\r\n])+)?$", RegexOptions.Compiled);
	
	/// <summary>
	/// Serialize the version as a string using <see cref="UnityVersionFormatFlags.Default"/>.
	/// </summary>
	/// <returns>A new string like 2019.4.3f1</returns>
	public override string ToString() => ToString(UnityVersionFormatFlags.Default);

	/// <summary>
	/// Serialize the version as a string
	/// </summary>
	/// <param name="flags">The flags to control how the version is formatted</param>
	/// <returns>A new string containing the formatted version.</returns>
	public string ToString(UnityVersionFormatFlags flags)
	{
		return (flags & UnityVersionFormatFlags.ExcludeType) != 0
			? ToStringWithoutType()
			: Type is not UnityVersionType.China || (flags & UnityVersionFormatFlags.UseShortChineseFormat) != 0
				? $"{Major}.{Minor}.{Build}{Type.ToCharacter()}{TypeNumber}"
				: $"{Major}.{Minor}.{Build}f1c{TypeNumber}";
	}

	/// <summary>
	/// Serialize the version as a string
	/// </summary>
	/// <param name="flags">The flags to control how the version is formatted</param>
	/// <param name="customEngineString">The custom engine string to be appended</param>
	/// <returns>A new string containing the formatted version.</returns>
	public string ToString(UnityVersionFormatFlags flags,
#if !NET6_0_OR_GREATER
		string customEngineString = "")
#else
		ReadOnlySpan<char> customEngineString = default)
#endif
	{
		return customEngineString.Length == 0
			? ToString(flags)
			: (flags & UnityVersionFormatFlags.ExcludeType) != 0
				? ToStringWithoutType()
				: Type is not UnityVersionType.China || (flags & UnityVersionFormatFlags.UseShortChineseFormat) != 0
					? $"{Major}.{Minor}.{Build}{Type.ToCharacter()}{TypeNumber}{customEngineString}"
					: $"{Major}.{Minor}.{Build}f1c{TypeNumber}{customEngineString}";
	}

	/// <summary>
	/// Serialize the version as a string using only <see cref="Major"/>, <see cref="Minor"/>, and <see cref="Build"/>.
	/// </summary>
	/// <returns>A new string like 2019.4.3</returns>
	public string ToStringWithoutType()
	{
		return $"{Major}.{Minor}.{Build}";
	}

	/// <summary>
	/// Parse a normal Unity version string
	/// </summary>
	/// <param name="version">A string to parse</param>
	/// <returns>The parsed Unity version</returns>
	/// <exception cref="ArgumentNullException">If the string is null or empty</exception>
	/// <exception cref="ArgumentException">If the string is in an invalid format</exception>
	public static UnityVersion Parse(string version) => Parse(version, out _);

	/// <summary>
	/// Parse a normal Unity version string
	/// </summary>
	/// <param name="s">A string to parse</param>
	/// <param name="customEngine">Not null if this version was generated by a custom Unity Engine.</param>
	/// <returns>The parsed Unity version</returns>
	/// <exception cref="ArgumentException">If the string is in an invalid format</exception>
	public static UnityVersion Parse(string s, out string? customEngine)
	{
		return TryParse(s, out UnityVersion version, out customEngine)
			? version
			: throw new ArgumentException($"Invalid version format: {s}", nameof(s));
	}

	/// <summary>
	/// Try to parse a normal Unity version string
	/// </summary>
	/// <param name="s">A string to parse</param>
	/// <param name="version">The parsed Unity version</param>
	/// <param name="customEngine">Not null if this version was generated by a custom Unity Engine.</param>
	/// <returns>True if parsing was successful</returns>
	public static bool TryParse(string s, out UnityVersion version, out string? customEngine)
	{
		if (string.IsNullOrEmpty(s))
		{
			customEngine = null;
			version = default;
			return false;
		}
		else if (chinaRegex.TryMatch(s, out Match? match))
		{
			int major = int.Parse(match.Groups[1].Value);
			int minor = int.Parse(match.Groups[2].Value);
			int build = int.Parse(match.Groups[3].Value);
			int typeNumber = int.Parse(match.Groups[4].Value);
			customEngine = GetNullableString(match.Groups[5]);
			version = new UnityVersion((ushort)major, (ushort)minor, (ushort)build, UnityVersionType.China, (byte)typeNumber);
			return true;
		}
		else if(normalRegex.TryMatch(s, out match))
		{
			int major = int.Parse(match.Groups[1].Value);
			int minor = int.Parse(match.Groups[2].Value);
			int build = int.Parse(match.Groups[3].Value);
			char type = match.Groups[4].Value[0];
			int typeNumber = int.Parse(match.Groups[5].Value);
			customEngine = GetNullableString(match.Groups[6]);
			version = new UnityVersion((ushort)major, (ushort)minor, (ushort)build, type.ToUnityVersionType(), (byte)typeNumber);
			return true;
		}
		else if (majorMinorBuildRegex.TryMatch(s, out match))
		{
			int major = int.Parse(match.Groups[1].Value);
			int minor = int.Parse(match.Groups[2].Value);
			int build = int.Parse(match.Groups[3].Value);
			customEngine = null;
			version = new UnityVersion((ushort)major, (ushort)minor, (ushort)build, UnityVersionType.Final, 1);
			return true;
		}
		else if (majorMinorRegex.TryMatch(s, out match))
		{
			int major = int.Parse(match.Groups[1].Value);
			int minor = int.Parse(match.Groups[2].Value);
			customEngine = null;
			version = new UnityVersion((ushort)major, (ushort)minor, 0, UnityVersionType.Final, 1);
			return true;
		}
		else
		{
			customEngine = null;
			version = default;
			return false;
		}

		static string? GetNullableString(Capture capture) => capture.Length == 0 ? null : capture.Value;
	}
}
