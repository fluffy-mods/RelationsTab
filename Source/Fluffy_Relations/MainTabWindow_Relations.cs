// Karel Kroeze
// MainTabWindow_Relations.cs
// 2016-12-26

using System.Collections.Generic;
using System.Linq;
using Fluffy_Relations.ForceDirectedGraph;
using RimWorld;
using UnityEngine;
using Verse;
using static Fluffy_Relations.Constants;

namespace Fluffy_Relations
{
    public enum GraphMode
    {
        ForceDirected,
        Circle
    }

    public enum Page
    {
        Colonists,
        Factions
    }

    public class MainTabWindow_Relations : MainTabWindow
    {
        #region Constructors

        public MainTabWindow_Relations()
        {
            forcePause = true;
        }

        #endregion Constructors

        #region Fields

        public Graph graph;
        private static Page _currentPage = Page.Colonists;
        private static GraphMode _mode = GraphMode.ForceDirected;
        private static Faction _selectedFaction;
        private static Pawn _selectedPawn;
        private static List<Pawn> pawns;
        private float _factionDetailHeight = 999f;
        private Vector2 _factionDetailScrollPosition = Vector2.zero;
        private float _factionInformationHeight = 999f;
        private Vector2 _factionInformationScrollPosition = Vector2.zero;
        private Pawn _lastSelectedPawn;
        private Rect detailRect;
        private Rect networkRect;
        private Rect sourceButtonRect;

        #endregion Fields

        #region Properties

        public Page CurrentPage
        {
            get => _currentPage;
            set
            {
                _currentPage = value;
                CreateGraph();
            }
        }

        public override Vector2 InitialSize => new Vector2( UI.screenWidth, UI.screenHeight - 35f );

        public Faction SelectedFaction
        {
            get => _selectedFaction;
            set
            {
                // unfreeze old selection
                if ( _mode == GraphMode.ForceDirected && _selectedFaction != null &&
                     graph.Node( _selectedFaction.Leader() ) != null )
                    graph.Node( _selectedFaction.Leader() ).Frozen = false;

                // change selection and freeze it if not null
                _selectedFaction = value;
                if ( value != null )
                    graph.Node( _selectedFaction.Leader() ).Frozen = true;

                // clear current list of connections
                graph.ClearEdges();

                // if something selected, draw only connections for that faction
                if ( value != null && graph.Node( value.Leader() ) != null )
                {
                    var node = graph.Node( value.Leader() );
                    foreach ( var other in graph.nodes )
                        graph.AddEdge<FactionEdge>( node, other );
                }
                else
                    // if nothing selected, build full list of connections
                {
                    foreach ( var node in graph.nodes )
                    foreach ( var other in graph.nodes )
                        graph.AddEdge<FactionEdge>( node, other );
                }

                // restart dynamic process
                graph.Restart();
            }
        }

        public Pawn SelectedPawn
        {
            get => _selectedPawn;
            set
            {
                // unfreeze old selection
                if ( _mode == GraphMode.ForceDirected && _selectedPawn != null && graph.Node( _selectedPawn ) != null )
                    graph.Node( _selectedPawn ).Frozen = false;

                // change selection and freeze it if not null
                _selectedPawn = value;
                if ( value != null )
                    graph.Node( _selectedPawn ).Frozen = true;

                // clear current list of connections
                graph.ClearEdges();

                // add opinions for currently selected
                if ( value != null )
                    foreach ( var other in _selectedPawn.GetRelatedPawns( pawns, true ) )
                        graph.AddEdge<PawnEdge>( graph.Node( _selectedPawn ), graph.Node( other ) );

                // add relations for all pawns
                foreach ( var node in graph.nodes )
                foreach ( var other in node.pawn.GetRelatedPawns( pawns, false ) )
                    graph.AddEdge<PawnEdge>( node, graph.Node( other ) );

                // start adaptive process
                graph.Restart();
            }
        }

        #endregion Properties

        #region Methods

        public void CreateCircle( bool freeze )
        {
            var count = graph.nodes.Count;
            var center = graph.Center;
            var radius = Mathf.Min( graph.size.x / 2f, graph.size.y / 2f ) - SlotSize / 2f;

            // set nodes on the circle, and freeze them
            for ( var i = 0; i < count; i++ )
            {
                var node = graph.nodes[i];
                node.position = Helpers.PointOnCircle( i, count, center, radius );
                node.Frozen = freeze;
            }
        }

        public void CreateGraph()
        {
            // calculate positions
            graph = new Graph( networkRect.size );
            if ( CurrentPage == Page.Colonists )
            {
                // initialize list of nodes
                graph.nodes = pawns.Select( pawn => new PawnNode( pawn, networkRect.RandomPoint(), graph ) as Node )
                    .ToList(); // note that we force the nodes in a circle regardless of this starting position
                foreach ( var node in graph.nodes )
                {
                    // attach event handlers to node
                    node.OnHover += () => TooltipHandler.TipRegion( node.slot, node.pawn.GetTooltip( SelectedPawn ) );
                    node.OnLeftClick += () => SelectedPawn = node.pawn;
                    node.PreDrawExtras += delegate
                    {
                        if ( node.pawn == SelectedPawn || Mouse.IsOver( node.slot ) )
                            GUI.DrawTexture( node.slot, Resources.Halo );
                    };
                    node.PostDrawExtras += delegate
                    {
                        if ( node.Frozen && _mode == GraphMode.ForceDirected )
                            GUI.DrawTexture(
                                new Rect( node.slot.xMax - 16f, node.slot.yMin,
                                    16f, 16f ), Resources.Pin );
                    };

                    // add edges - assign SelectedPawn to null to trigger Set method and reset selected
                    SelectedPawn = null;
                }
            }
            else
            {
                graph.nodes = Find.FactionManager
                    .AllFactionsInViewOrder
                    .Select( f => new FactionNode( f, networkRect.RandomPoint(), graph ) as Node )
                    .Where( n => n.pawn != null )
                    .ToList();

                foreach ( var node in graph.nodes )
                {
                    // attach event handlers to node
                    var fnode = node as FactionNode;
                    if ( fnode == null )
                    {
                        Log.Warning( "Non-faction node in node list for faction tab. " );
                        continue;
                    }

                    fnode.OnHover +=
                        () => TooltipHandler.TipRegion( fnode.slot, fnode.faction.GetTooltip( SelectedFaction ) );
                    fnode.OnLeftClick += () => SelectedFaction = fnode.faction;
                    fnode.PreDrawExtras += delegate
                    {
                        if ( fnode.faction == SelectedFaction || Mouse.IsOver( fnode.slot ) )
                            GUI.DrawTexture( fnode.slot, Resources.Halo );
                    };
                    node.PostDrawExtras += delegate
                    {
                        if ( node.Frozen && _mode == GraphMode.ForceDirected )
                            GUI.DrawTexture( new Rect( node.slot.xMax - 16f, node.slot.yMin, 16f, 16f ),
                                Resources.Pin );
                    };
                    node.PostDrawExtras += delegate
                    {
                        var factionIconRect = new Rect( node.slot.xMin, node.slot.yMin, 16f, 16f );
                        PawnSlotDrawer.DrawTextureColoured( factionIconRect, fnode.faction.def.ExpandingIconTexture, fnode.faction.Color );
                    };

                    // attach edges - assign selected to itself to trigger Set method.
                    SelectedFaction = null;
                }
            }

            // force circle positions if mode is circle
            CreateCircle( _mode == GraphMode.Circle );
        }

        public override void DoWindowContents( Rect canvas )
        {
            // update the graph
            graph.Update();

            // set size and draw background
            base.DoWindowContents( canvas );

            // source selection button
            DrawSourceButton();

            // graph reset and mode selection icons
            DrawGraphOptions( canvas );

            // draw relevant page
            if ( CurrentPage == Page.Colonists )
                DrawPawnRelations();
            if ( CurrentPage == Page.Factions )
                DrawFactionRelations();

            // see if we can catch clicks in the main rect to reset selections
            if ( Widgets.ButtonInvisible( networkRect ) )
            {
                if ( CurrentPage == Page.Colonists )
                    SelectedPawn = null;
                if ( CurrentPage == Page.Factions )
                    SelectedFaction = null;
            }
        }

        public void DrawDetails( Rect canvas, Pawn pawn )
        {
            GUI.BeginGroup( canvas );

            var numSections = 3;
            var titleHeight = 30f;
            var margin = 6f;
            var availableHeight = canvas.height - ( titleHeight + margin ) * numSections;

            // set up rects
            var pawnInfoTitleRect = new Rect( 0f, 0f, canvas.width, titleHeight );
            var pawnInfoRect = new Rect( 0f, titleHeight + margin, canvas.width, availableHeight / 5f );
            var relationsTitleRect = new Rect( 0f, pawnInfoRect.yMax, canvas.width, titleHeight );
            var relationsRect = new Rect( 0f, relationsTitleRect.yMax + margin, canvas.width,
                availableHeight / 5f * 2f );
            var interactionsTitleRect = new Rect( 0f, relationsRect.yMax + margin, canvas.width, titleHeight );
            var interactionsRect = new Rect( 0f, interactionsTitleRect.yMax + margin, canvas.width,
                availableHeight / 5f * 2f );

            // titles
            Text.Font = GameFont.Medium;
            Widgets.Label( pawnInfoTitleRect, pawn.Name.ToStringFull );
            Widgets.Label( relationsTitleRect,
                "Fluffy_Relations.Possesive".Translate( pawn.LabelShort ) +
                "Fluffy_Relations.Relations".Translate() );
            Widgets.Label( interactionsTitleRect,
                "Fluffy_Relations.Possesive".Translate( pawn.LabelShort ) +
                "Fluffy_Relations.Interactions".Translate() );
            Text.Font = GameFont.Small;

            // draw overview of traits and status effects relevant to social relations
            RelationsHelper.DrawSocialStatusEffectsSummary( pawnInfoRect, pawn );

            // draw relations overview.
            SocialCardUtility.DrawRelationsAndOpinions( relationsRect, pawn );

            // need to call log drawer through reflection. Geez.
            InteractionCardUtility.DrawInteractionsLog( interactionsRect, pawn, Find.PlayLog.AllEntries, 24 );
            GUI.EndGroup();
        }

        public void DrawDetails( Rect canvas, Faction faction )
        {
            // set up rects
            var informationIconRect = new Rect( 0f, 0f, 30f, 30f );
            var informationTitleRect = new Rect( 2 * (informationIconRect.width + 6f ), 0f, canvas.width, 30f );
            var informationRect = new Rect( 0f, 36f, canvas.width, canvas.height / 2f - 36f );
            var informationViewRect = new Rect( 0f, 0f, informationRect.width - 16f, _factionInformationHeight );
            var relationsTitleRect = new Rect( 0f, canvas.height / 2f, canvas.width, 30f );
            var relationsRect = new Rect( 0f, canvas.height / 2f + 36f, canvas.width, canvas.height / 2f - 36f );
            var relationsViewRect = new Rect( 0f, 0f, relationsRect.width - 16f, _factionDetailHeight );

            GUI.BeginGroup( canvas );

            // draw faction icons
            PawnSlotDrawer.DrawTextureColoured( informationIconRect, faction.def.ExpandingIconTexture, faction.Color );
            informationIconRect.x += 36f;
            Texture2D baseTexture;
            if ( Resources.baseTextures.TryGetValue( faction.def, out baseTexture ) )
                PawnSlotDrawer.DrawTextureColoured( informationIconRect, baseTexture, faction.Color );

            // draw titles
            Text.Font = GameFont.Medium;
            Widgets.Label( informationTitleRect, faction.GetFactionLabel() );
            Widgets.Label( relationsTitleRect,
                "Fluffy_Relations.Possesive".Translate( faction.GetFactionLabel() ) +
                "Fluffy_Relations.Relations".Translate() );
            Text.Font = GameFont.Small;

            // information
            Widgets.BeginScrollView( informationRect, ref _factionInformationScrollPosition, informationViewRect );
            var curY = 0f;

            var factionLeaderRect = new Rect( 0f, curY, informationRect.width, RowHeight );
            if ( faction == Faction.OfPlayer )
            {
                factionLeaderRect.xMin += RowHeight + Inset;
                Rect factionLeaderSelectRect = new Rect( 0f, curY + ( RowHeight - SmallIconSize ) / 2f, SmallIconSize, SmallIconSize);
                TooltipHandler.TipRegion( factionLeaderSelectRect, "Fluffy_Relations.SelectLeaderTip".Translate() );
                if ( Widgets.ButtonImage( factionLeaderSelectRect, Resources.Edit ) )
                {
                    // do leader selection dropdown.
                    var options = new List<FloatMenuOption>();

                    // pawns on maps
                    foreach ( var pawn in Find.Maps.SelectMany( m => m.mapPawns.FreeColonists ) )
                    {
                        // todo; draw portrait extra.
                        options.Add(new FloatMenuOption(pawn.NameStringShort, () =>
                        {
                            GameComponent_Leader.Leader = pawn;
                            BuildPawnList(); // restarts graph
                        },
                        extraPartWidth: 24f,
                        extraPartOnGUI: ( rect ) =>
                        {
                            GUI.DrawTexture( rect, PortraitsCache.Get( pawn, new Vector2( rect.width, rect.height ) ) );
                            return Widgets.ButtonInvisible( rect );
                        }));
                    }

                    // pawns in caravans
                    foreach ( var pawn in Find.WorldObjects.Caravans
                        .Where( c => c.IsPlayerControlled )
                        .SelectMany( c => c.PawnsListForReading )
                        .Where( p => p.IsColonist) )
                    {
                        // todo; draw portrait extra.
                        options.Add(new FloatMenuOption(pawn.NameStringShort, () =>
                            {
                                GameComponent_Leader.Leader = pawn;
                                BuildPawnList(); // restarts graph
                            },
                            extraPartWidth: 24f,
                            extraPartOnGUI: (rect) =>
                            {
                                GUI.DrawTexture(rect, PortraitsCache.Get(pawn, new Vector2(rect.width, rect.height)));
                                return Widgets.ButtonInvisible(rect);
                            }));
                    }

                    Find.WindowStack.Add( new FloatMenu( options ) );
                }
            }
            curY += RowHeight;
            var factionTypeRect = new Rect( 0f, curY, informationRect.width, RowHeight );
            curY += RowHeight;
            var factionDescriptionRect = new Rect( 0f, curY, informationRect.width,
                Text.CalcHeight( $"<i>{faction.def.description}</i>", informationRect.width ) );
            var kidnappedRect = new Rect( 0f, curY, informationRect.width, RowHeight );
            curY += RowHeight;

            Widgets.Label( factionTypeRect, faction.def.LabelCap + " (" + faction.def.techLevel + ")" );
            Widgets.Label( factionLeaderRect, faction.def.leaderTitle + ": " + ( faction.Leader()?.Name.ToStringFull ?? "Noone".Translate() ) );
            Widgets.Label( factionDescriptionRect, $"<i>{faction.def.description}</i>" );
            if ( faction.kidnapped?.KidnappedPawnsListForReading.Count > 0 )
            {
                Widgets.Label( kidnappedRect, "Fluffy_Relations.KidnappedColonists".Translate() + ":" );
                foreach ( var kidnappee in faction.kidnapped.KidnappedPawnsListForReading )
                {
                    var kidnappeeRow = new Rect( 0f, curY, informationRect.width, RowHeight );
                    curY += RowHeight;

                    Widgets.Label( kidnappeeRow, "\t" + kidnappee.Name );
                }
            }

            _factionInformationHeight = curY;
            Widgets.EndScrollView();

            // relations
            Widgets.BeginScrollView( relationsRect, ref _factionDetailScrollPosition, relationsViewRect );
            curY = 0f;

            foreach ( var otherFaction in Find.FactionManager
                .AllFactionsVisible
                .Where( other => other != faction &&
                                 other.RelationWith( faction, true ) != null )
                .OrderByDescending( of => of.GoodwillWith( faction ) ) )
            {
                var row = new Rect( 0f, curY, canvas.width, RowHeight );
                curY += RowHeight;

                var opinion = Mathf.RoundToInt( otherFaction.GoodwillWith( faction ) );
                GUI.color = RelationsHelper.GetRelationColor( opinion );
                var label = "";
                if ( faction.HostileTo( otherFaction ) )
                    label = "HostileTo".Translate( otherFaction.GetCallLabel() );
                else
                    label = otherFaction.GetFactionLabel();
                label += ": " + opinion;

                Widgets.DrawHighlightIfMouseover( row );
                Widgets.Label( row, label );
                if ( Widgets.ButtonInvisible( row ) )
                    SelectedFaction = otherFaction;
            }

            // reset color
            GUI.color = Color.white;

            _factionDetailHeight = curY;
            Widgets.EndScrollView(); // relations

            GUI.EndGroup(); // canvas
        }

        public void DrawFactionRelations()
        {
            // draw that graph
            graph.Draw( networkRect );

            // draw legend or details in the detail rect
            if ( SelectedFaction != null )
                DrawDetails( detailRect, SelectedFaction );
            else
                DrawLegend( detailRect );
        }

        public void DrawLegend( Rect canvas )
        {
            // TODO: Draw legend.
            Text.Anchor = TextAnchor.MiddleCenter;
            GUI.color = Color.grey;
            Widgets.Label( canvas, "Fluffy_Relations.NothingSelected".Translate() );
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
        }

        public void DrawPawnRelations()
        {
            // catch selected pawn changes ( clicking on relations and/or log entries will move selector )
            UpdateSelectedPawn();

            // draw pawn graph
            graph.Draw( networkRect );

            // draw legend or details in the detail rect
            if ( SelectedPawn != null )
                DrawDetails( detailRect, SelectedPawn );
            else
                DrawLegend( detailRect );
        }

        public override void PostClose()
        {
            base.PostClose();

            _lastSelectedPawn = null;
        }

        public override void PreOpen()
        {
            base.PreOpen();
            BuildPawnList();
            _selectedFaction = Faction.OfPlayer;
            _selectedPawn = pawns.FirstOrDefault();
        }

        public void UpdateSelectedPawn()
        {
            // Clicking on the opinion overviews selects a pawn in the game's selector).
            // which is what we're trying to catch and reflect here.
            var selectorPawn = Find.Selector.SingleSelectedThing as Pawn;

            // do we currently have a pawn selected, and is it different from our previous selection?
            if ( selectorPawn != null && selectorPawn != _lastSelectedPawn )
            {
                // is the pawn we currently have selected a valid target for selection in the relations tab?
                // (i.e. is it a colonist?)
                if ( RelationsHelper.Colonists.Contains( selectorPawn ) )
                    SelectedPawn = selectorPawn;

                // stop this check happening again until we select something else.
                _lastSelectedPawn = selectorPawn;
            }
        }

        /// <summary>
        ///     Builds pawn list + slot positions
        ///     called from base.PreOpen();
        /// </summary>
        protected void BuildPawnList()
        {
            // rebuild pawn list
            pawns = Find.VisibleMap.mapPawns.FreeColonists.ToList();
            RelationsHelper.ResetOpinionCache();

            // recalculate positions
            CreateAreas();
            CreateGraph();

            // create list of social thoughts to pawns
            RelationsHelper.CreateThoughtList( pawns );
        }

        // split the screen into two areas
        private void CreateAreas()
        {
            // social network on the right, always square, try to fill the whole height - but limited by width.
            var desiredNetworkSize = Mathf.Min( UI.screenHeight - 35f, UI.screenWidth - MinDetailWidth ) -
                                     2 * Margin;

            // detail view on the left, full height (minus what is needed for faction/colonists selection) - fill available width, but don't exceed 1/3 of the screen
            var detailRectWidth = Mathf.Min( UI.screenWidth - desiredNetworkSize - Margin * 2, Screen.width / 3f );
            detailRect = new Rect( 0f, 36f, detailRectWidth, UI.screenHeight - 35f - Margin * 2 );

            // finalize the network rect
            networkRect = new Rect( detailRectWidth + Margin * 2, 0f, desiredNetworkSize, desiredNetworkSize );

            // selection button rect
            sourceButtonRect = new Rect( 0f, 0f, 200f, 30f );
        }

        private Rect GetIconRect( Rect canvas, int index )
        {
            return new Rect(canvas.xMax - ( IconSize + Inset ) * index, canvas.yMin + Inset, IconSize, IconSize );
        }

        private void DrawGraphOptions( Rect canvas )
        {
            var iconIndex = 1;
            if ( _mode == GraphMode.ForceDirected )
            {
                // tooltips
                var modeIconRect = GetIconRect(canvas, iconIndex++);
                var resetIconRect = GetIconRect(canvas, iconIndex++);
                TooltipHandler.TipRegion( modeIconRect, "Fluffy_Relations.ModeCircleTip".Translate() );
                TooltipHandler.TipRegion( resetIconRect, "Fluffy_Relations.GraphResetTip".Translate() );

                if ( Widgets.ButtonImage(modeIconRect, Resources.DotsCircle ) )
                {
                    _mode = GraphMode.Circle;
                    BuildPawnList(); // restarts graph
                }

                if ( Widgets.ButtonImage(resetIconRect, TexUI.RotLeftTex ) )
                    BuildPawnList();
            }
            if ( _mode == GraphMode.Circle )
            {
                var modeIconRect = GetIconRect(canvas, iconIndex++);
                TooltipHandler.TipRegion(modeIconRect, "Fluffy_Relations.ModeGraphTip".Translate() );

                if ( Widgets.ButtonImage(modeIconRect, Resources.DotsDynamic ) )
                {
                    _mode = GraphMode.ForceDirected;
                    BuildPawnList(); // restarts graph
                }
            }
        }

        private void DrawSourceButton()
        {
            // draw source selection rect

            // set game font to small (otherwise fully zoomed in the fonts go tiny.)
            Text.Font = GameFont.Small;
            if ( CurrentPage == Page.Colonists )
                if ( Widgets.ButtonText( sourceButtonRect, "Fluffy_Relations.Colonists".Translate() ) )
                    CurrentPage = Page.Factions;
            if ( CurrentPage == Page.Factions )
                if ( Widgets.ButtonText( sourceButtonRect, "Fluffy_Relations.Factions".Translate() ) )
                    CurrentPage = Page.Colonists;
            TooltipHandler.TipRegion( sourceButtonRect, "Fluffy_Relations.SourceButtonTip".Translate() );
        }

        #endregion Methods
    }
}