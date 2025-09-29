public static class StringColorExtension {
    /// <summary>
    /// Converts the text into red color
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static string ToAqua(this string str) {
        return string.Format("<color=#00ffffff>{0}</color>", str);

    }
    public static string ToBlack(this string str) {
        return string.Format("<color=#000000ff>{0}</color>", str);

    }
    public static string ToBlue(this string str) {
        return string.Format("<color=#0000ffff>{0}</color>", str);

    }
    public static string ToBrown(this string str) {
        return string.Format("<color=#a52a2aff>{0}</color>", str);
    }
    public static string ToCyan(this string str) {
        return string.Format("<color=#00ffffff>{0}</color>", str);
    }
    public static string ToDarkBlue(this string str) {
        return string.Format("<color=#0000a0ff>{0}</color>", str);
    }
    public static string ToFuchsia(this string str) {
        return string.Format("<color=#ff00ffff>{0}</color>", str);
    }
    public static string ToGreen(this string str) {
        return string.Format("<color=#008000ff>{0}</color>", str);
    }
    public static string ToLightGreen(this string str) {
        return string.Format("<color=#bfff00ff>{0}</color>", str);
    }
    public static string ToLightGreen2(this string str) {
        return string.Format("<color=#ccff00ff>{0}</color>", str);
    }
    public static string ToGrey(this string str) {
        return string.Format("<color=#808080ff>{0}</color>", str);
    }
    public static string ToLightBlue(this string str) {
        return string.Format("<color=#add8e6ff>{0}</color>", str);
    }
    public static string ToLime(this string str) {
        return string.Format("<color=#00ff00ff>{0}</color>", str);
    }
    public static string ToMagenta(this string str) {
        return string.Format("<color=#ff00ffff>{0}</color>", str);
    }
    public static string ToMaroon(this string str) {
        return string.Format("<color=#800000ff>{0}</color>", str);
    }
    public static string ToNavy(this string str) {
        return string.Format("<color=#000080ff>{0}</color>", str);
    }
    public static string ToOlive(this string str) {
        return string.Format("<color=#808000ff>{0}</color>", str);
    }
    public static string ToOrange(this string str) {
        return string.Format("<color=#ffa500ff>{0}</color>", str);
    }
    public static string ToPurple(this string str) {
        return string.Format("<color=#800080ff>{0}</color>", str);
    }
    public static string ToAsh(this string str) {
        return string.Format("<color=#A1A1A1>{0}</color>", str);
    }
    public static string ToRed(this string str) {
        return string.Format("<color=#ff0000ff>{0}</color>", str);
    }
    public static string ToSilver(this string str) {
        return string.Format("<color=#c0c0c0ff>{0}</color>", str);
    }
    public static string ToTeal(this string str) {
        return string.Format("<color=#008080ff>{0}</color>", str);
    }
    public static string ToWhite(this string str) {
        return string.Format("<color=#ffffffff>{0}</color>", str);
    }
    public static string ToYellow(this string str) {
        return string.Format("<color=#ffff00ff>{0}</color>", str);
    }

    /// <summary>
    /// Converts the text into Bold format 
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static string ToBold(this string str) {
        return string.Format("<b>{0}</b>", str);

    }
    /// <summary>
    /// Converts the text into Italic format 
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static string ToItalic(this string str) {
        return string.Format("<i>{0}</i>", str);

    }
    /// <summary>
    /// Alters the string size
    /// </summary>
    /// <param name="str"></param>
    /// <param name="size"></param>
    /// <returns></returns>
    public static string ToSize(this string str, int size) {
        var redefined = UnityEngine.Mathf.Clamp(size, 1, 25);
        return string.Format("<size={0}>{1}</size>", size, str);

    }
}
