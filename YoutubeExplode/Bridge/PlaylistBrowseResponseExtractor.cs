using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using YoutubeExplode.Utils;
using YoutubeExplode.Utils.Extensions;

namespace YoutubeExplode.Bridge;

internal partial class PlaylistBrowseResponseExtractor : IPlaylisBrowsertExtractor
{
    private readonly JsonElement _content;

    public PlaylistBrowseResponseExtractor(JsonElement content) => _content = content;

    private JsonElement? TryGetPlaylistRoot() => Memo.Cache(this, () =>
        _content
            .GetPropertyOrNull("contents")?
            .GetPropertyOrNull("twoColumnBrowseResultsRenderer")?
            .GetPropertyOrNull("tabs")?
            .EnumerateArrayOrNull()?
            .ElementAtOrNull(0)?
            .GetPropertyOrNull("tabRenderer")?
            .GetPropertyOrNull("content")?
            .GetPropertyOrNull("sectionListRenderer")?
            .GetPropertyOrNull("contents")?
            .EnumerateArrayOrNull()?
            .ElementAtOrNull(0)?
            .GetPropertyOrNull("itemSectionRenderer")?
            .GetPropertyOrNull("contents")?
            .EnumerateArrayOrNull()?
            .ElementAtOrNull(0)?
            .GetPropertyOrNull("playlistVideoListRenderer")
    );

    public IReadOnlyList<PlaylistVideoExtractor> GetVideos() => Memo.Cache(this, () =>
        TryGetPlaylistRoot()?
            .GetPropertyOrNull("contents")?
            .EnumerateArrayOrNull()?
            .Select(j => j.GetPropertyOrNull("playlistVideoRenderer"))
            .WhereNotNull()
            .Select(j => new PlaylistVideoExtractor(j))
            .ToArray() ??

        Array.Empty<PlaylistVideoExtractor>()
    );

    public bool IsPlaylistAvailable() => Memo.Cache(this, () =>
        TryGetSidebar() is not null
    );

    private JsonElement? TryGetSidebar() => Memo.Cache(this, () =>
        _content
            .GetPropertyOrNull("sidebar")?
            .GetPropertyOrNull("playlistSidebarRenderer")?
            .GetPropertyOrNull("items")
    );

    private JsonElement? TryGetSidebarPrimary() => Memo.Cache(this, () =>
        TryGetSidebar()?
            .EnumerateArrayOrNull()?
            .ElementAtOrNull(0)?
            .GetPropertyOrNull("playlistSidebarPrimaryInfoRenderer")
    );

    private JsonElement? TryGetSidebarSecondary() => Memo.Cache(this, () =>
        TryGetSidebar()?
            .EnumerateArrayOrNull()?
            .ElementAtOrNull(1)?
            .GetPropertyOrNull("playlistSidebarSecondaryInfoRenderer")
    );

    public string? TryGetClickTracking() => Memo.Cache(this, () =>
     TryGetPlaylistRoot()?
         .GetPropertyOrNull("contents")?
         .EnumerateArrayOrNull()?
         .LastOrNull()?
         .GetPropertyOrNull("continuationItemRenderer")?
         .GetPropertyOrNull("continuationEndpoint")?
         .GetPropertyOrNull("clickTrackingParams")?
         .GetStringOrNull()
    );

    public string? TryGetToken() => Memo.Cache(this, () =>
        TryGetPlaylistRoot()?
            .GetPropertyOrNull("contents")?
            .EnumerateArrayOrNull()?
            .LastOrNull()?
            .GetPropertyOrNull("continuationItemRenderer")?
            .GetPropertyOrNull("continuationEndpoint")?
            .GetPropertyOrNull("continuationCommand")?
                .GetPropertyOrNull("token")?
                .GetStringOrNull()
    );

    public string? TryGetPlaylistTitle() => Memo.Cache(this, () =>
        TryGetSidebarPrimary()?
            .GetPropertyOrNull("title")?
            .GetPropertyOrNull("simpleText")?
            .GetStringOrNull() ??

        TryGetSidebarPrimary()?
            .GetPropertyOrNull("title")?
            .GetPropertyOrNull("runs")?
            .EnumerateArrayOrNull()?
            .Select(j => j.GetPropertyOrNull("text")?.GetStringOrNull())
            .WhereNotNull()
            .ConcatToString()
    );

    private JsonElement? TryGetPlaylistAuthorDetails() => Memo.Cache(this, () =>
        TryGetSidebarSecondary()?
            .GetPropertyOrNull("videoOwner")?
            .GetPropertyOrNull("videoOwnerRenderer")
    );

    public string? TryGetPlaylistAuthor() => Memo.Cache(this, () =>
        TryGetPlaylistAuthorDetails()?
            .GetPropertyOrNull("title")?
            .GetPropertyOrNull("simpleText")?
            .GetStringOrNull() ??

        TryGetPlaylistAuthorDetails()?
            .GetPropertyOrNull("title")?
            .GetPropertyOrNull("runs")?
            .EnumerateArrayOrNull()?
            .Select(j => j.GetPropertyOrNull("text")?.GetStringOrNull())
            .WhereNotNull()
            .ConcatToString()
    );

    public string? TryGetPlaylistChannelId() => Memo.Cache(this, () =>
        TryGetPlaylistAuthorDetails()?
            .GetPropertyOrNull("navigationEndpoint")?
            .GetPropertyOrNull("browseEndpoint")?
            .GetPropertyOrNull("browseId")?
            .GetStringOrNull()
    );

    public string? TryGetPlaylistDescription() => Memo.Cache(this, () =>
        TryGetSidebarPrimary()?
            .GetPropertyOrNull("description")?
            .GetPropertyOrNull("simpleText")?
            .GetStringOrNull() ??

        TryGetSidebarPrimary()?
            .GetPropertyOrNull("description")?
            .GetPropertyOrNull("runs")?
            .EnumerateArrayOrNull()?
            .Select(j => j.GetPropertyOrNull("text")?.GetStringOrNull())
            .WhereNotNull()
            .ConcatToString()
    );

    public IReadOnlyList<ThumbnailExtractor> GetPlaylistThumbnails() => Memo.Cache(this, () =>
        TryGetSidebarPrimary()?
            .GetPropertyOrNull("thumbnailRenderer")?
            .GetPropertyOrNull("playlistVideoThumbnailRenderer")?
            .GetPropertyOrNull("thumbnail")?
            .GetPropertyOrNull("thumbnails")?
            .EnumerateArrayOrNull()?
            .Select(j => new ThumbnailExtractor(j))
            .ToArray() ??

        TryGetSidebarPrimary()?
            .GetPropertyOrNull("thumbnailRenderer")?
            .GetPropertyOrNull("playlistCustomThumbnailRenderer")?
            .GetPropertyOrNull("thumbnail")?
            .GetPropertyOrNull("thumbnails")?
            .EnumerateArrayOrNull()?
            .Select(j => new ThumbnailExtractor(j))
            .ToArray() ??

        Array.Empty<ThumbnailExtractor>()
    );
}

internal partial class PlaylistBrowseResponseExtractor
{
    public static PlaylistBrowseResponseExtractor Create(string raw) => new(Json.Parse(raw));
}