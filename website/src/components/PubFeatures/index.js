import React from 'react';
import clsx from 'clsx';
import Link from '@docusaurus/Link';
import styles from './styles.module.css';

export function PubCard({ title, authors, conference, url, pdf, openReview, year, highlight }) {
  return (
    <div className={clsx('card', styles.pubCard)}>
      <div className={styles.cardBody}>
        
        {highlight && (
          <div className={styles.badgeContainer}>
            <span className={styles.highlightBadge}>{highlight}</span>
          </div>
        )}

        <h3 className={styles.title}>
          <Link 
            to={url} 
            className={styles.titleLink}
          >
            {title}
          </Link>
        </h3>

        <div className={styles.authorRow}>
          {authors.map((author, index) => (
            <span key={index}>
              {author.url ? (
                <Link 
                  to={author.url} 
                  className={styles.authorLink}
                  target="_blank" 
                  rel="noopener noreferrer"
                >
                  {author.name}
                </Link>
              ) : (
                <span>{author.name}</span>
              )}
              {index < authors.length - 1 ? ", " : ""}
            </span>
          ))}
        </div>

        <div className={styles.venueRow}>
          <span className={styles.venue}>{conference}</span>, {year}.
        </div>

      </div>

      {(pdf || openReview) && (
        <div className={styles.cardFooter}>
          {pdf && (
            <Link 
              to={pdf}
              className={clsx("button button--outline button--primary button--sm", styles.actionButton)}
            >
              PDF
            </Link>
          )}
          {openReview && (
            <Link 
              to={openReview}
              className={clsx("button button--outline button--primary button--sm", styles.actionButton)}
            >
              Open Review
            </Link>
          )}
        </div>
      )}
    </div>
  );
}

export function PubBrowser({ data }) {
  return (
    <div className={styles.pubList}>
      {data.map((props, idx) => (
        <PubCard key={idx} {...props} />
      ))}
    </div>
  );
}