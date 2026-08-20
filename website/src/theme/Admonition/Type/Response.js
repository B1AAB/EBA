import React from 'react';
import clsx from 'clsx';
import Details from '@theme/Details';
import IconResponse from '../Icon/Response';
import styles from './styles.module.css';

// Colors of `alert--response` are defined in src/css/custom.css. `@theme/Details`
// hardcodes `alert alert--info`, hence the higher-specificity override there.
const infimaClassName = 'alert--response';

export default function AdmonitionTypeResponse({
  title,
  icon,
  className,
  children,
  ...props
}) {
  return (
    <Details
      {...props}
      className={clsx(infimaClassName, className)}
      summary={
        <summary className={styles.responseHeading}>
          <span className={styles.responseIcon}>{icon ?? <IconResponse />}</span>
          {title ?? 'response'}
        </summary>
      }>
      {children}
    </Details>
  );
}
