import clsx from 'clsx';
import styles from './styles.module.css';
import WholeRowFeature from './wholeRowFeature.js';
import Feature from './feature.js';
//import styles from '@site/src/css/index.module.css';

const ApplicationsList = [
  {
    title: 'Anomaly Detection',
    //Svg: require('@site/static/img/undraw_docusaurus_mountain.svg').default,
    description: (
      <>
        Identify unusual transaction patterns, detect potentially illicit activities, 
        or pinpoint network vulnerabilities by leveraging the graph's 
        detailed temporal and structural information.
      </>
    ),
  },
  {
    title: 'Entity Profiling',
    //Svg: require('@site/static/img/undraw_docusaurus_tree.svg').default,
    description: (
      <>
        Analyze transaction histories and script interactions to profile 
        different types of Bitcoin entities (e.g., exchanges, miners, services), 
        track their evolution, and understand their economic impact within the ecosystem.
      </>
    ),
  },
  {
    title: 'Graph ML Benchmarking',
    //Svg: require('@site/static/img/undraw_docusaurus_react.svg').default,
    description: (
      <>
        Utilize this massive, real-world graph as a challenging 
        benchmark dataset to evaluate the performance, scalability, 
        and effectiveness of new large-scale graph machine learning models and algorithms.
      </>
    ),
  },
];

const economicEvolutionFeatures = [
  {
    title: <>Model the financial flows and network topology of Bitcoin's on-chain economy using our freely available temporal graph.</>,
    description: (
      <>
        Trace 16 years of economic evolution with our complete temporal heterogeneous graph to reveal the on-chain dynamics and behavioral patterns that static datasets obscure.
      </>
    ),
    buttons: [
      {
        buttonLink: '/docs/bitcoin/datasets/overview',
        buttonText: 'Download Graph'
      },
      {
        buttonLink: '/docs/bitcoin/datasets/overview',
        buttonText: 'Download Sampled Communities'
      },
      {
        buttonLink: '/docs/bitcoin/datasets/overview',
        buttonText: 'Download Block Metadata'
      },
      {
        buttonLink: '/docs/bitcoin/overview',
        buttonText: 'Find Your Starting Point'
      }
    ],
    colSize: "col--12",
  }
];

const largestDatasetFeatures = [
  {
    title: <>Largest Public Graph Dataset</>,
    description: (
      <>
        Test the limits of your models on the largest publicly available
        temporal graph of the Bitcoin blockchain.
        With over 2.4 billion nodes and 39.7 billion edges,
        this dataset provides a massive, real-world benchmark
        to validate the performance and scalability of your models.
      </>
    ),
    buttons: [
      {
        buttonLink: '/docs/bitcoin/datasets/raw',
        buttonText: 'Download Graph Dataset'
      }
    ],
    colSize: "col--12",
  }
];

const mlReadyDesignFeatures = [
  {
    title: <>ML-Ready Design</>,
    description: (
      <>
        Our graph abstracts away the complexities of Bitcoin's UTXO ledger, 
        providing an intuitive representation of fund flows 
        so you can go straight to designing and training your models.
      </>
    ),
    colSize: "col--12",
  }
];

const toolkitFeatures = [
  {
    title: <>A Complete and Accessible ML Workflow</>,
    description: (
      <>
        Get started quickly with a full suite of resources. 
        We provide the complete ETL pipeline for full data reproducibility, 
        a customizable sampling method for creating custom subgraphs, 
        a collection of pre-built models in ready-to-use Jupyter Notebooks, 
        and external annotations for training or validating your models.
      </>
    ),
    colSize: "col--12",
  }
];

export default function HomepageFeatures() {
  return (
    <>
    {economicEvolutionFeatures && economicEvolutionFeatures.length > 0 && (
        <section className={clsx(styles.features)}>
          <div className="container">
            <div className={clsx('row', 'single-feature-row')}>
              {economicEvolutionFeatures.map((props, idx) => (
                <WholeRowFeature
                  key={idx}
                  {...props}
                  contentAlignment="center"
                  imageAlignment="center"
                />
              ))}
            </div>
          </div>
        </section>
      )}

    {largestDatasetFeatures && largestDatasetFeatures.length > 0 && (
        <section className={clsx(styles.featuresAlt)}>
          <div className="container">
            <div className={clsx('row', 'single-feature-row')}>
              {largestDatasetFeatures.map((props, idx) => (
                <WholeRowFeature
                  key={idx}
                  {...props}
                  contentAlignment="center"
                  imageAlignment="center"
                />
              ))}
            </div>
          </div>
        </section>
      )
    }

    {mlReadyDesignFeatures && mlReadyDesignFeatures.length > 0 && (
        <section className={clsx(styles.features)}>
          <div className="container">
            <div className={clsx('row', 'single-feature-row')}>
              {mlReadyDesignFeatures.map((props, idx) => (
                <WholeRowFeature
                  key={idx}
                  {...props}
                  contentAlignment="center"
                  imageAlignment="center"
                />
              ))}
            </div>
          </div>
        </section>
      )
    }

    {toolkitFeatures && toolkitFeatures.length > 0 && (
        <section className={clsx(styles.featuresAlt)}>
          <div className="container">
            <div className={clsx('row', 'single-feature-row')}>
              {toolkitFeatures.map((props, idx) => (
                <WholeRowFeature
                  key={idx}
                  {...props}
                  contentAlignment="center"
                  imageAlignment="center"
                />
              ))}
            </div>
          </div>
        </section>
      )
    }
    
    {
      <section className={styles.features}>
        <div className="container">
          <div className="row">
            {ApplicationsList.map((props, idx) => (
              <Feature key={idx} {...props} />
            ))}
          </div>
        </div>
      </section>
    }
  </>
  );
}
