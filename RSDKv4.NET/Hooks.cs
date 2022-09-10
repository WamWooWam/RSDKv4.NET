using System;
using System.Collections.Generic;
using System.Text;

namespace RSDKv4
{
    // todo: what eventargs would be helpful for these events
    public class Hooks
    {
        /// <summary>
        /// Occurs after game configuration data is loaded from the RSDK.
        /// </summary>
        public event EventHandler GameConfigLoaded;

        /// <summary>
        /// Occurs at the start of a frame where <see cref="Scene.stageMode"/> is <see cref="STAGEMODE.LOAD"/>. 
        /// Usually when a level is loading.
        /// </summary>
        public event EventHandler StageWillLoad;

        /// <summary>
        /// Occurs at the end of a frame where <see cref="Scene.stageMode"/> is <see cref="STAGEMODE.LOAD"/>
        /// Usually once a level has finished loading.
        /// </summary>
        public event EventHandler StageDidLoad;

        /// <summary>
        /// Occurs at the start of a frame where <see cref="Scene.stageMode"/> is <see cref="STAGEMODE.NORMAL"/> or <see cref="STAGEMODE.NORMAL_STEP"/>.
        /// Usually during gameplay.
        /// </summary>
        public event EventHandler StageWillStep;

        /// <summary>
        /// Occurs at the end of a frame where <see cref="Scene.stageMode"/> is <see cref="STAGEMODE.NORMAL"/> or <see cref="STAGEMODE.NORMAL_STEP"/>.
        /// Usually during gameplay.
        /// </summary>
        public event EventHandler StageDidStep;

        internal void OnStageWillLoad()
        {
            StageWillLoad?.Invoke(this, EventArgs.Empty);
        }

        internal void OnStageDidLoad()
        {
            StageDidLoad?.Invoke(this, EventArgs.Empty);
        }

        internal void OnStageWillStep()
        {
            StageWillStep?.Invoke(this, EventArgs.Empty);
        }

        internal void OnStageDidStep()
        {
            StageDidStep?.Invoke(this, EventArgs.Empty);
        }

        internal void OnGameConfigLoaded()
        {
            GameConfigLoaded?.Invoke(this, EventArgs.Empty);
        }
    }
}
