import React from 'react';
import clsx from 'clsx';
import AdmonitionLayout from '@theme/Admonition/Layout';
import IconPrompt from '../Icon/Prompt';

// Colors of `alert--prompt` are defined in src/css/custom.css,
// following how Infima defines them for the built-in admonitions.
const infimaClassName = 'alert alert--prompt';

const defaultProps = {
  icon: <IconPrompt />,
  title: 'prompt',
};

export default function AdmonitionTypePrompt(props) {
  return (
    <AdmonitionLayout
      {...defaultProps}
      {...props}
      className={clsx(infimaClassName, props.className)}>
      {props.children}
    </AdmonitionLayout>
  );
}
