const TsconfigPathsPlugin = require('tsconfig-paths-webpack-plugin');
const ESLintPlugin = require('eslint-webpack-plugin');
const TerserPlugin = require('terser-webpack-plugin');
const webpack = require('webpack');

module.exports = {
	mode: 'development',
	entry: './src/app.ts',
	output: {
		path: __dirname,
		filename: './www/assets/app.js'
	},
	plugins: [
		new webpack.IgnorePlugin({
			resourceRegExp: /(fs|child_process)/
		}),
		new webpack.ProvidePlugin({
			$: 'jquery',
			jQuery: 'jquery',
			'window.$': 'jquery',
			'window.jQuery': 'jquery',
		}),
		new ESLintPlugin({
			extensions: ['ts', 'tsx'],
			eslintPath: require.resolve('eslint'),
			overrideConfigFile: '.eslintrc.js',
		}),
	],
	resolve: {
		plugins: [
			new TsconfigPathsPlugin(),
		],
		extensions: ['.ts', '.tsx', '.js'],
	},
	module: {
		rules: [
			{
				test: /\.tsx?$/,
				use: [
					{
						loader: 'ts-loader',
					},
				],
				exclude: /node_modules/,
			},
			{
				test: /\.html$/,
				type: 'asset/source',
			},
			{
				test: /\.css$/i,
				use: ['style-loader', 'css-loader'],
			},
			{
				test: /\.scss$/,
				use: [
					'style-loader',
					'css-loader',
					{
						loader: 'sass-loader',
						options: {
							implementation: require('sass'),
							api: 'modern',
							sassOptions: {
								loadPaths: ['node_modules'],
							},
						},
					},
				],
			},
		],
	},
	performance: {
		hints: false,
	},
	optimization: {
		minimizer: [
			new TerserPlugin({
				extractComments: false,
			}),
		],
	},
};
