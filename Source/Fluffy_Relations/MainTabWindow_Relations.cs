using Fluffy_Relations.ForceDirectedGraph;
using RimWorld;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

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

    public class MainTabWindow_Relations : MainTabWindow_PawnList
    {
        #region Fields

        public Graph graph;
        private static Page _currentPage = Page.Colonists;
        private static GraphMode _mode = GraphMode.ForceDirected;
        private static Faction _selectedFaction;
        private static Pawn _selectedPawn;
        private float _factionDetailHeight = 999f;
        private Vector2 _factionDetailScrollPosition = Vector2.zero;
        private float _factionInformationHeight = 999f;
        private Vector2 _factionInformationScrollPosition = Vector2.zero;
        private Pawn _lastSelectedPawn;
        private Rect detailRect;
        private Rect networkRect;
        private Rect sourceButtonRect;

        #endregion Fields

        #region Constructors

        public MainTabWindow_Relations()
        {
            forcePause = true;
        }

        #endregion Constructors

        #region Properties

        public Page CurrentPage
        {
            get
            {
                return _currentPage;
            }
            set
            {
                _currentPage = value;
                CreateGraph();
            }
        }

        public override Vector2 InitialSize
        {
            get
            {
                return new Vector2( Screen.width, Screen.height - 35f );
            }
        }

        public Faction SelectedFaction
        {
            get
            {
                return _selectedFaction;
            }
            set
            {
                // unfreeze old selection
                if ( _mode == GraphMode.ForceDirected && _selectedFaction != null && graph.Node( _selectedFaction.Leader() ) != null )
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
                    Node node = graph.Node( value.Leader() );
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
            get
            {
                return _selectedPawn;
            }
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
                {
                    foreach ( var other in _selectedPawn.GetRelatedPawns( pawns, true ) )
                    {
                        graph.AddEdge<PawnEdge>( graph.Node( _selectedPawn ), graph.Node( other ) );
                    }
                }

                // add relations for all pawns
                foreach ( var node in graph.nodes )
                {
                    foreach ( var other in node.pawn.GetRelatedPawns( pawns, false ) )
                    {
                        graph.AddEdge<PawnEdge>( node, graph.Node( other ) );
                    }
                }

                // start adaptive process
                graph.Restart();
            }
        }

        #endregion Properties

        #region Methods

        public void CreateCircle()
        {
            int count = graph.nodes.Count;
            Vector2 center = graph.Center;
            float radius = Mathf.Min( graph.size.x / 2f, graph.size.y / 2f ) - Settings.SlotSize / 2f;

            // set nodes on the circle, and freeze them
            for ( int i = 0; i < count; i++ )
            {
                Node node = graph.nodes[i];
                node.position = Helpers.PointOnCircle( i, count, center, radius );
                node.Frozen = true;
            }
        }

        public void CreateGraph()
        {
            // calculate positions
            graph = new Graph( networkRect.size );
            if ( CurrentPage == Page.Colonists )
            {
                // initialize list of nodes
                graph.nodes = pawns.Select( pawn => new PawnNode( pawn, networkRect.RandomPoint(), graph ) as Node ).ToList();
                foreach ( Node node in graph.nodes )
                {
                    // attach event handlers to node
                    node.OnHover += new Action( () => TooltipHandler.TipRegion( node.slot, node.pawn.GetTooltip( SelectedPawn ) ) );
                    node.OnLeftClick += new Action( () => SelectedPawn = node.pawn );
                    node.PreDrawExtras += delegate
                    {
                        if ( node.pawn == SelectedPawn || Mouse.IsOver( node.slot ) )
                            GUI.DrawTexture( node.slot, Resources.Halo );
                    };
                    node.PostDrawExtras += delegate
                    {
                        if ( node.Frozen && _mode == GraphMode.ForceDirected )
                            GUI.DrawTexture( new Rect( node.slot.xMax - 16f, node.slot.yMin, 16f, 16f ), Resources.Pin );
                    };

                    // add edges - assign SelectedPawn to null to trigger Set method and reset selected
                    SelectedPawn = null;
                }
            }
            else
            {
                graph.nodes = Find.FactionManager.AllFactionsInViewOrder.Select( f => new FactionNode( f, networkRect.RandomPoint(), graph ) as Node ).ToList();
                foreach ( var node in graph.nodes )
                {
                    // attach event handlers to node
                    FactionNode fnode = node as FactionNode;
                    if ( fnode == null )
                    {
                        Log.Warning( "Non-faction node in node list for faction tab. " );
                        continue;
                    }

                    fnode.OnHover += new Action( () => TooltipHandler.TipRegion( fnode.slot, fnode.faction.GetTooltip( SelectedFaction ) ) );
                    fnode.OnLeftClick += new Action( () => SelectedFaction = fnode.faction );
                    fnode.PreDrawExtras += delegate
                    {
                        if ( fnode.faction == SelectedFaction || Mouse.IsOver( fnode.slot ) )
                            GUI.DrawTexture( fnode.slot, Resources.Halo );
                    };
                    node.PostDrawExtras += delegate
                    {
                        if ( node.Frozen && _mode == GraphMode.ForceDirected )
                            GUI.DrawTexture( new Rect( node.slot.xMax - 16f, node.slot.yMin, 16f, 16f ), Resources.Pin );
                    };

                    // attach edges - assign selected to itself to trigger Set method.
                    SelectedFaction = null;
                }
            }

            // force circle positions if mode is circle
            if ( _mode == GraphMode.Circle )
                CreateCircle();
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

            int numSections = 3;
            float titleHeight = 30f;
            float margin = 6f;
            float availableHeight = canvas.height - ( titleHeight + margin ) * numSections;

            // set up rects
            Rect pawnInfoTitleRect = new Rect( 0f, 0f, canvas.width, titleHeight );
            Rect pawnInfoRect = new Rect( 0f, titleHeight + margin, canvas.width, availableHeight / 5f );
            Rect relationsTitleRect = new Rect( 0f, pawnInfoRect.yMax, canvas.width, titleHeight );
            Rect relationsRect = new Rect( 0f, relationsTitleRect.yMax + margin, canvas.width, availableHeight / 5f * 2f );
            Rect interactionsTitleRect = new Rect( 0f, relationsRect.yMax + margin, canvas.width, titleHeight );
            Rect interactionsRect = new Rect( 0f, interactionsTitleRect.yMax + margin, canvas.width, availableHeight / 5f * 2f );

            // titles
            Text.Font = GameFont.Medium;
            Widgets.Label( pawnInfoTitleRect, pawn.Name.ToStringFull );
            Widgets.Label( relationsTitleRect, "Fluffy_Relations.Possesive".Translate( pawn.LabelShort ) + "Fluffy_Relations.Relations".Translate() );
            Widgets.Label( interactionsTitleRect, "Fluffy_Relations.Possesive".Translate( pawn.LabelShort ) + "Fluffy_Relations.Interactions".Translate() );
            Text.Font = GameFont.Small;

            // draw overview of traits and status effects relevant to social relations
            RelationsHelper.DrawSocialStatusEffectsSummary( pawnInfoRect, pawn );

            // draw relations overview.
            SocialCardUtility.DrawRelationsAndOpinions( relationsRect, pawn );

            // need to call log drawer through reflection. Geez.
            Resources.DrawSocialLogMI.Invoke( null, new object[] { interactionsRect, pawn } );

            GUI.EndGroup();
        }

        public void DrawDetails( Rect canvas, Faction faction )
        {
            // set up rects
            Rect informationTitleRect = new Rect( 0f, 0f, canvas.width, 30f );
            Rect informationRect = new Rect( 0f, 36f, canvas.width, canvas.height / 2f - 36f );
            Rect informationViewRect = new Rect( 0f, 0f, informationRect.width - 16f, _factionInformationHeight );
            Rect relationsTitleRect = new Rect( 0f, canvas.height / 2f, canvas.width, 30f );
            Rect relationsRect = new Rect( 0f, canvas.height / 2f + 36f, canvas.width, canvas.height / 2f - 36f );
            Rect relationsViewRect = new Rect( 0f, 0f, relationsRect.width - 16f, _factionDetailHeight );

            GUI.BeginGroup( canvas );

            // draw titles
            Text.Font = GameFont.Medium;
            Widgets.Label( informationTitleRect, faction.GetCallLabel() );
            Widgets.Label( relationsTitleRect, "Fluffy_Relations.Possesive".Translate( faction.GetCallLabel() ) + "Fluffy_Relations.Relations".Translate() );
            Text.Font = GameFont.Small;

            // information
            Widgets.BeginScrollView( informationRect, ref _factionInformationScrollPosition, informationViewRect );
            float curY = 0f;

            Rect factionTypeRect = new Rect( 0f, curY, informationRect.width, Settings.RowHeight );
            curY += Settings.RowHeight;
            Rect factionLeaderRect = new Rect( 0f, curY, informationRect.width, Settings.RowHeight );
            curY += Settings.RowHeight;
            Rect kidnappedRect = new Rect( 0f, curY, informationRect.width, Settings.RowHeight );
            curY += Settings.RowHeight;

            Widgets.Label( factionTypeRect, faction.def.LabelCap + " (" + faction.def.techLevel + ")" );
            Widgets.Label( factionLeaderRect, faction.def.leaderTitle + ": " + faction.Leader().Name );
            if ( faction.kidnapped?.KidnappedPawnsListForReading.Count > 0 )
            {
                Widgets.Label( kidnappedRect, "Fluffy_Relations.KidnappedColonists".Translate() + ":" );
                foreach ( Pawn kidnappee in faction.kidnapped.KidnappedPawnsListForReading )
                {
                    Rect kidnappeeRow = new Rect( 0f, curY, informationRect.width, Settings.RowHeight );
                    curY += Settings.RowHeight;

                    Widgets.Label( kidnappeeRow, "\t" + kidnappee.Name );
                }
            }

            _factionInformationHeight = curY;
            Widgets.EndScrollView();

            // relations
            Widgets.BeginScrollView( relationsRect, ref _factionDetailScrollPosition, relationsViewRect );
            curY = 0f;

            foreach ( Faction otherFaction in Find.FactionManager
                                                  .AllFactionsVisible
                                                  .Where( of => of != faction &&
                                                          of.RelationWith( faction, true ) != null )
                                                  .OrderByDescending( of => of.GoodwillWith( faction ) ) )
            {
                {
                    Rect row = new Rect( 0f, curY, canvas.width, Settings.RowHeight );
                    curY += Settings.RowHeight;

                    int opinion = Mathf.RoundToInt( faction.GoodwillWith( otherFaction ) );
                    GUI.color = RelationsHelper.GetRelationColor( opinion );
                    string label = "";
                    if ( faction.HostileTo( otherFaction ) )
                        label = "HostileTo".Translate( otherFaction.GetCallLabel() );
                    else
                        label = otherFaction.GetCallLabel();
                    label += ": " + opinion;

                    Widgets.DrawHighlightIfMouseover( row );
                    Widgets.Label( row, label );
                    if ( Widgets.ButtonInvisible( row ) )
                        SelectedFaction = otherFaction;
                }
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
        
        public void UpdateSelectedPawn()
        {
            if ( Find.Selector.SingleSelectedThing as Pawn != _lastSelectedPawn )
            {
                SelectedPawn = Find.Selector.SingleSelectedThing as Pawn;
                _lastSelectedPawn = SelectedPawn;
            }
        }

        /// <summary>
        /// Builds pawn list + slot positions
        /// called from base.PreOpen();
        /// </summary>
        protected override void BuildPawnList()
        {
            // rebuild pawn list
            base.BuildPawnList();
            RelationsHelper.ResetOpinionCache();

            // recalculate positions
            CreateAreas();
            CreateGraph();

            // create list of social thoughts to pawns
            RelationsHelper.CreateThoughtList( pawns );
        }

        protected override void DrawPawnRow( Rect r, Pawn p )
        {
            // required implementation
            return;
        }

        // split the screen into two areas
        private void CreateAreas()
        {
            // social network on the right, always square, try to fill the whole height - but limited by width.
            float networkSize = Mathf.Min( Screen.height - 35f, Screen.width - Settings.MinDetailWidth ) - 2 * Margin;
            networkRect = new Rect( 0f, 0f, networkSize, networkSize );
            networkRect.x = Screen.width - networkSize - 2 * Margin;

            // detail view on the left, full height (minus what is needed for faction/colonists selection) - fill available width
            detailRect = new Rect( 0f, 36f, Screen.width - networkSize - Margin * 2, Screen.height - 35f - Margin * 2 );

            // selection button rect
            sourceButtonRect = new Rect( 0f, 0f, 200f, 30f );
        }

        private void DrawGraphOptions( Rect canvas )
        {
            // draw graph mode selector and reset button
            Rect modeSelectRect = new Rect( canvas.xMax - Settings.IconSize - Settings.Inset, canvas.yMin + Settings.Inset, Settings.IconSize, Settings.IconSize );
            Rect graphResetRect = new Rect( canvas.xMax - ( Settings.IconSize + Settings.Inset ) * 2, canvas.yMin + Settings.Inset, Settings.IconSize, Settings.IconSize );

            if ( _mode == GraphMode.ForceDirected )
            {
                // tooltips
                TooltipHandler.TipRegion( modeSelectRect, "Fluffy_Relations.ModeCircleTip".Translate() );
                TooltipHandler.TipRegion( graphResetRect, "Fluffy_Relations.GraphResetTip".Translate() );

                if ( Widgets.ButtonImage( modeSelectRect, Resources.DotsCircle ) )
                {
                    _mode = GraphMode.Circle;
                    BuildPawnList(); // restarts graph
                }

                if ( Widgets.ButtonImage( graphResetRect, TexUI.RotLeftTex ) )
                {
                    BuildPawnList();
                }
            }
            if ( _mode == GraphMode.Circle )
            {
                TooltipHandler.TipRegion( modeSelectRect, "Fluffy_Relations.ModeGraphTip".Translate() );

                if ( Widgets.ButtonImage( modeSelectRect, Resources.DotsDynamic ) )
                {
                    _mode = GraphMode.ForceDirected;
                    BuildPawnList(); // restarts graph
                }
            }
        }

        private void DrawSourceButton()
        {
            // draw source selection rect
            if ( CurrentPage == Page.Colonists )
            {
                if ( Widgets.ButtonText( sourceButtonRect, "Fluffy_Relations.Colonists".Translate() ) )
                {
                    CurrentPage = Page.Factions;
                }
            }
            if ( CurrentPage == Page.Factions )
            {
                if ( Widgets.ButtonText( sourceButtonRect, "Fluffy_Relations.Factions".Translate() ) )
                {
                    CurrentPage = Page.Colonists;
                }
            }
            TooltipHandler.TipRegion( sourceButtonRect, "Fluffy_Relations.SourceButtonTip".Translate() );
        }

        #endregion Methods
    }
}