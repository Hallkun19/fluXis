using System.Collections.Generic;
using fluXis.Game.Database.Maps;

namespace fluXis.Game.Utils;

public class SearchFilters
{
    public List<string> SearchTerms { get; set; } = new();

    public int BPM { get; set; }
    public Type BPMType { get; set; }

    public int Status { get; set; } = -5;

    public static SearchFilters Create(string search)
    {
        var filter = new SearchFilters();

        foreach (var split in search.ToLower().Trim().Split(" "))
        {
            if (split.StartsWith("bpm"))
            {
                var bpm = split.Substring(3);
                Type bpmType;

                if (bpm.StartsWith("="))
                    bpmType = Type.Exact;
                else if (bpm.StartsWith(">"))
                    bpmType = Type.Over;
                else if (bpm.StartsWith("<"))
                    bpmType = Type.Under;
                else
                    continue;

                bpm = bpm.Substring(1);

                if (int.TryParse(bpm, out var bpmValue))
                {
                    filter.BPM = bpmValue;
                    filter.BPMType = bpmType;
                }
            }
            else if (split.StartsWith("status="))
            {
                var status = split.Substring(7);

                switch (status)
                {
                    case "l" or "local":
                        filter.Status = -2;
                        break;

                    case "u" or "unsubmitted":
                        filter.Status = 0;
                        break;

                    case "p" or "pending":
                        filter.Status = 1;
                        break;

                    case "i" or "impure":
                        filter.Status = 2;
                        break;

                    case "p" or "pure":
                        filter.Status = 3;
                        break;
                }
            }
            else
                filter.SearchTerms.Add(split);
        }

        return filter;
    }

    public bool Matches(RealmMap map)
    {
        bool matches = false;

        if (BPM > 0)
        {
            switch (BPMType)
            {
                case Type.Exact:
                {
                    if (map.BPMMin <= BPM && map.BPMMax >= BPM)
                        matches = true;
                    else
                        return false;

                    break;
                }

                case Type.Over:
                {
                    if (map.BPMMin > BPM)
                        matches = true;
                    else
                        return false;

                    break;
                }

                case Type.Under:
                {
                    if (map.BPMMax < BPM)
                        matches = true;
                    else
                        return false;

                    break;
                }
            }
        }

        if (Status != -5)
        {
            if (map.Status == Status)
                matches = true;
            else
                return false;
        }

        if (SearchTerms.Count > 0)
        {
            foreach (var term in SearchTerms)
            {
                bool termMatches = false;

                string title = map.Metadata.Title.ToLower();
                string artist = map.Metadata.Artist.ToLower();
                string mapper = map.Metadata.Mapper.ToLower();
                string source = map.Metadata.Source.ToLower();
                string tags = map.Metadata.Tags.ToLower();
                string difficulty = map.Difficulty.ToLower();

                termMatches |= title.Contains(term);
                termMatches |= artist.Contains(term);
                termMatches |= mapper.Contains(term);
                termMatches |= source.Contains(term);
                termMatches |= tags.Contains(term);
                termMatches |= difficulty.Contains(term);

                if (!termMatches)
                    return false;

                if (!matches)
                    matches = true;
            }
        }

        return matches;
    }

    public enum Type
    {
        Exact,
        Over,
        Under,
    }
}