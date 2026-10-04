import {useEffect, useRef, useState} from 'react';

import styles from './styles.module.css';

const PHRASES = ['every payment', 'every peel chain', 'every mixer', 'every market move'];
const PHRASE_HOLD_MS = 2000;
const PHRASE_SLIDE_MS = 600;

// Each phrase holds for PHRASE_HOLD_MS, then slides out right as the next
// slides in, while the slot eases to the new phrase's width.
export default function RotatingPhrase() {
  const [{current, previous}, setPhrases] = useState({current: 0, previous: null});
  const sizers = useRef([]);
  const [widths, setWidths] = useState(null);

  useEffect(() => {
    const id = setInterval(
      () => setPhrases(({current: c}) => ({current: (c + 1) % PHRASES.length, previous: c})),
      PHRASE_HOLD_MS + PHRASE_SLIDE_MS,
    );
    return () => clearInterval(id);
  }, []);

  // Re-measured whenever a phrase's rendered width changes: font load, resize.
  useEffect(() => {
    const measure = () => setWidths(sizers.current.map((el) => el.getBoundingClientRect().width));
    const observer = new ResizeObserver(measure);
    sizers.current.forEach((el) => observer.observe(el));
    return () => observer.disconnect();
  }, []);

  return (
    <>
      <span
        className={`${styles.phraseSlot} ${styles.accent}`}
        style={widths ? {width: widths[current]} : undefined}>
        {previous !== null && (
          <span key={`out-${previous}-${current}`} className={styles.phraseOut} aria-hidden="true">
            {PHRASES[previous]}
          </span>
        )}
        <span key={`in-${current}`} className={previous === null ? undefined : styles.phraseIn}>
          {PHRASES[current]}
        </span>
      </span>
      <span className={styles.phraseSizers} aria-hidden="true">
        {PHRASES.map((phrase, i) => (
          <span key={phrase} ref={(el) => (sizers.current[i] = el)}>
            {phrase}
          </span>
        ))}
      </span>
    </>
  );
}
