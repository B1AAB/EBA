import {useEffect, useRef, useState} from 'react';
import Link from '@docusaurus/Link';
import useBaseUrl from '@docusaurus/useBaseUrl';
import useDocusaurusContext from '@docusaurus/useDocusaurusContext';
import graphViz from '@site/src/data/graphViz';
import RotatingPhrase from './RotatingPhrase';

import styles from './styles.module.css';

function GraphOverlay() {
  return (
    <div className={styles.graphOverlay}>
      <p className={styles.overlayText}>
        <span className={styles.highlight}>
          <span className={styles.accent}>EBA</span> empowers
        </span>
        <span className={styles.highlight}>your model with</span>
        <span className={styles.highlight}>
          <RotatingPhrase />
        </span>
        <span className={styles.highlight}>in Bitcoin&apos;s</span>
        <span className={styles.highlight}>complete history.</span>
      </p>
    </div>
  );
}

// The widget is prebuilt into static/graph-viz (see graph-viz/build.mjs) and
// loaded at runtime, so webpack must not try to bundle it.
export default function HomepageHero() {
  const {siteConfig} = useDocusaurusContext();
  const ref = useRef(null);
  const widgetBase = useBaseUrl('/graph-viz/');
  const dataUrl = useBaseUrl('/graph/sample.json');
  const [failed, setFailed] = useState(false);

  useEffect(() => {
    let cancelled = false;
    let unmount;
    const css = document.createElement('link');
    css.rel = 'stylesheet';
    css.href = `${widgetBase}${graphViz.style}`;
    document.head.appendChild(css);
    import(/* webpackIgnore: true */ `${widgetBase}${graphViz.script}`)
      .then((widget) => widget.mount(ref.current, {dataUrl}))
      .then((remove) => {
        if (cancelled) remove();
        else unmount = remove;
      })
      .catch((err) => {
        console.error('Graph visualization failed to load', err);
        if (!cancelled) setFailed(true);
      });
    return () => {
      cancelled = true;
      unmount?.();
      css.remove();
    };
  }, [widgetBase, dataUrl]);

  return (
    <header className={styles.graphHero}>
      <div className={styles.graphStage}>
        {failed ? (
          <div className={styles.graphFallback}>{siteConfig.tagline}</div>
        ) : (
          <div ref={ref} className={`eba-graph ${styles.graph}`} />
        )}
        <GraphOverlay />
      </div>
      <div className={styles.graphCaption}>
        <span>
          This graph shows a portion of EBA’s Bitcoin graph related to Individual X.
          {' '}
          <Link to="https://www.youtube.com/watch?v=327Q97uo4pw">
            Watch the story behind this slice.
          </Link>{' '}
          Drag to orbit, Ctrl + scroll to zoom, and hover to inspect.
        </span>
        <Link className="button button--secondary" to="/docs/gs/quickstart">
          Quick Start
        </Link>
      </div>
    </header>
  );
}
