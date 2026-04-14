namespace woliver13.DiplomacyAdjudicator.Core.Domain;

public enum Season { Spring, Fall }

public enum PhaseType { Movement, Retreat, Build }

public record GamePhase(Season Season, int Year, PhaseType Type);
